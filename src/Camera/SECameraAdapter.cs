using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Interfaces;
using VRageMath;

namespace SECinematicCamera.Camera
{
    public struct CameraWriteDiagnostics
    {
        public Vector3D PositionBeforeWrite;
        public Vector3D PositionAfterWrite;
        public string SpectatorMovement;
        public bool PoseRewrittenThisTick;
    }

    public sealed class SECameraAdapter
    {
        private sealed class CameraState
        {
            public MyEntity Entity;
            public bool FirstPerson;
            public double ThirdPersonDistance;
            public MySpectatorCameraMovementEnum SpectatorMovement;
        }

        private CameraState _savedState;
        private CameraState _observedView;
        private IMyCameraController _expectedController;
        private CameraWriteDiagnostics _lastWrite;

        public bool HasCapturedState { get { return _savedState != null; } }
        public string CapturedMovementName
        {
            get { return _savedState != null ? _savedState.SpectatorMovement.ToString() : "Unavailable"; }
        }

        public bool TryEnterFixed(CameraPose initialPose, out string reason)
        {
            reason = null;
            if (MyAPIGateway.Session == null || MySpectator.Static == null)
            {
                reason = "Camera services are unavailable.";
                return false;
            }

            CaptureCurrentState();
            try
            {
                // Set this explicitly. Selecting SpectatorFixed alone does not reliably
                // prevent the controlled ship's input from also driving MySpectator.
                MySpectator.Static.SpectatorCameraMovement = MySpectatorCameraMovementEnum.None;
                MyAPIGateway.Session.SetCameraController(MyCameraControllerEnum.SpectatorFixed, null, initialPose.Position);
                MySpectator.Static.SpectatorCameraMovement = MySpectatorCameraMovementEnum.None;
                _expectedController = MyAPIGateway.Session.CameraController;
                if (_expectedController == null)
                    throw new System.InvalidOperationException("SpectatorFixed did not provide a camera controller.");

                ApplyPose(initialPose);
                return true;
            }
            catch (System.Exception exception)
            {
                reason = "Unable to enter SpectatorFixed: " + exception.Message;
                Restore();
                return false;
            }
        }

        public bool HasExpectedCameraController()
        {
            return MyAPIGateway.Session != null && _expectedController != null &&
                   object.ReferenceEquals(MyAPIGateway.Session.CameraController, _expectedController) &&
                   MySpectator.Static != null &&
                   MySpectator.Static.SpectatorCameraMovement == MySpectatorCameraMovementEnum.None;
        }

        public void ApplyPose(CameraPose pose)
        {
            MySpectator spectator = MySpectator.Static;
            if (spectator == null)
                throw new System.InvalidOperationException("Spectator camera became unavailable.");

            _lastWrite.PoseRewrittenThisTick = false;
            _lastWrite.PositionBeforeWrite = spectator.Position;

            // The cinematic pose is authoritative and absolute. Never integrate from
            // the spectator's current position, which may have received user input.
            spectator.SpectatorCameraMovement = MySpectatorCameraMovementEnum.None;
            spectator.Position = pose.Position;
            spectator.SetTarget(pose.LookAt, pose.Up);

            _lastWrite.PositionAfterWrite = spectator.Position;
            _lastWrite.SpectatorMovement = spectator.SpectatorCameraMovement.ToString();
            _lastWrite.PoseRewrittenThisTick = true;
        }

        public CameraWriteDiagnostics GetLastWriteDiagnostics()
        {
            return _lastWrite;
        }

        public CameraProjectionInfo GetProjectionInfo()
        {
            if (MyAPIGateway.Session == null || MyAPIGateway.Session.Camera == null)
                return new CameraProjectionInfo(1.0, 0.75);

            MatrixD projection = MyAPIGateway.Session.Camera.ProjectionMatrix;
            double horizontal = System.Math.Abs(projection.M11) > 0.0001
                ? 1.0 / System.Math.Abs(projection.M11)
                : 1.0;
            double vertical = System.Math.Abs(projection.M22) > 0.0001
                ? 1.0 / System.Math.Abs(projection.M22)
                : 0.75;
            return new CameraProjectionInfo(horizontal, vertical);
        }

        public void Restore(bool forceControlledView = false)
        {
            CameraState state = _savedState;
            bool ownsCamera = HasExpectedCameraController();
            _savedState = null;
            _expectedController = null;
            if (state == null || MyAPIGateway.Session == null ||
                MyAPIGateway.Session.LocalHumanPlayer == null)
                return;

            // Never reclaim a camera another system has taken. On a seat change,
            // restore only the currently controlled entity, never the old cockpit.
            if (!ownsCamera)
                return;
            MyEntity currentEntity = GetCurrentControlledEntity();
            bool sameEntity = currentEntity != null && currentEntity == state.Entity;
            bool firstPerson = forceControlledView || !sameEntity || state.FirstPerson;
            TrySetEntityView(currentEntity, firstPerson,
                sameEntity ? state.ThirdPersonDistance : 0.0);

            RestoreSpectatorMovement(state);
        }

        public bool RecoverControlledCockpitFromSpectator()
        {
            if (MyAPIGateway.Session == null || MyAPIGateway.Session.LocalHumanPlayer == null)
                return false;

            IMyControllableEntity controlled = MyAPIGateway.Session.ControlledObject;
            MyEntity entity = controlled != null ? controlled.Entity as MyEntity : null;
            if (!(entity is IMyShipController))
                return false;

            IMyCameraController camera = MyAPIGateway.Session.CameraController;
            if (camera != null && camera.Entity != null)
                return false;

            return TrySetEntityView(entity, true, 0.0);
        }

        public bool HasEntityCameraController()
        {
            IMyCameraController camera = MyAPIGateway.Session != null
                ? MyAPIGateway.Session.CameraController
                : null;
            return camera != null && camera.Entity != null;
        }

        public void ClearTransientState()
        {
            _expectedController = null;
            _lastWrite = default(CameraWriteDiagnostics);
        }

        private static void RestoreSpectatorMovement(CameraState state)
        {
            if (MySpectator.Static != null)
                MySpectator.Static.SpectatorCameraMovement = state.SpectatorMovement;
        }

        public void ObservePlayerView()
        {
            if (MyAPIGateway.Session == null || MyAPIGateway.Session.LocalHumanPlayer == null)
                return;
            MyEntity entity = GetCurrentControlledEntity();
            IMyCameraController controller = MyAPIGateway.Session.CameraController;
            if (entity != null && controller != null && controller.Entity == entity)
                _observedView = ReadEntityView(entity, controller);
        }

        private void CaptureCurrentState()
        {
            MyEntity entity = GetCurrentControlledEntity();
            IMyCameraController controller = MyAPIGateway.Session.CameraController;
            CameraState view = controller != null && controller.Entity == entity
                ? ReadEntityView(entity, controller)
                : _observedView != null && _observedView.Entity == entity
                    ? _observedView : ReadEntityView(entity, entity as IMyCameraController);
            _savedState = new CameraState
            {
                Entity = entity,
                FirstPerson = view.FirstPerson,
                ThirdPersonDistance = view.ThirdPersonDistance,
                SpectatorMovement = MySpectator.Static.SpectatorCameraMovement
            };
        }

        private static CameraState ReadEntityView(MyEntity entity, IMyCameraController controller)
        {
            return new CameraState
            {
                Entity = entity,
                FirstPerson = controller == null || controller.IsInFirstPersonView,
                ThirdPersonDistance = MyAPIGateway.Session.CameraTargetDistance
            };
        }

        private static MyEntity GetCurrentControlledEntity()
        {
            IMyControllableEntity controlled = MyAPIGateway.Session != null
                ? MyAPIGateway.Session.ControlledObject
                : null;
            return controlled != null ? controlled.Entity as MyEntity : null;
        }

        private static bool TrySetEntityView(MyEntity entity, bool firstPerson, double distance)
        {
            if (entity == null || entity.Closed || entity.MarkedForClose ||
                MyAPIGateway.Session == null)
                return false;

            try
            {
                IMyCameraController entityCamera = entity as IMyCameraController;
                if (entityCamera != null && entityCamera.ForceFirstPersonCamera)
                    firstPerson = true;
                if (entityCamera != null)
                    entityCamera.IsInFirstPersonView = firstPerson;

                MyAPIGateway.Session.SetCameraController(MyCameraControllerEnum.Entity, entity);
                IMyCameraController activeCamera = MyAPIGateway.Session.CameraController;
                if (activeCamera != null)
                    activeCamera.IsInFirstPersonView = firstPerson;
                if (!firstPerson && distance > 0.0 && !double.IsInfinity(distance) && !double.IsNaN(distance))
                    MyAPIGateway.Session.CameraTargetDistance = distance;
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }
    }
}
