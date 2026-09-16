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
            public MySpectatorCameraMovementEnum SpectatorMovement;
        }

        private CameraState _savedState;
        private IMyCameraController _expectedController;
        private CameraWriteDiagnostics _lastWrite;

        public bool HasCapturedState { get { return _savedState != null; } }
        public string CapturedMovementName
        {
            get { return _savedState != null ? _savedState.SpectatorMovement.ToString() : "Unavailable"; }
        }

        public bool TryEnterFixed(CameraPose initialPose, bool preferControlledView,
            out string reason)
        {
            reason = null;
            if (MyAPIGateway.Session == null || MySpectator.Static == null)
            {
                reason = "Camera services are unavailable.";
                return false;
            }

            CaptureCurrentState(preferControlledView);
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
            _savedState = null;
            _expectedController = null;
            if (MyAPIGateway.Session == null)
                return;

            // A deterministic first-person fallback is safer than restoring a
            // stale cockpit or spectator controller. In particular, leaving a
            // cockpit changes ControlledObject to the character while the saved
            // cockpit remains valid; restoring that old cockpit makes the next V
            // press jump back to a camera the player no longer controls.
            MyEntity currentEntity = GetCurrentControlledEntity();
            if (!TrySetEntityFirstPerson(currentEntity) && state != null)
                TrySetEntityFirstPerson(state.Entity);

            if (state != null)
                RestoreSpectatorMovement(state);
        }

        public bool RecoverControlledCockpitFromSpectator()
        {
            if (MyAPIGateway.Session == null)
                return false;

            IMyControllableEntity controlled = MyAPIGateway.Session.ControlledObject;
            MyEntity entity = controlled != null ? controlled.Entity as MyEntity : null;
            if (!(entity is IMyShipController))
                return false;

            IMyCameraController camera = MyAPIGateway.Session.CameraController;
            if (camera != null && camera.Entity != null)
                return false;

            return TrySetEntityFirstPerson(entity);
        }

        public bool HasEntityCameraController()
        {
            IMyCameraController camera = MyAPIGateway.Session != null
                ? MyAPIGateway.Session.CameraController
                : null;
            return camera != null && camera.Entity != null;
        }

        public bool RestoreCurrentControlledFirstPerson()
        {
            return TrySetEntityFirstPerson(GetCurrentControlledEntity());
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

        private void CaptureCurrentState(bool preferControlledView)
        {
            IMyCameraController controller = MyAPIGateway.Session.CameraController;
            MyEntity entity = controller != null ? controller.Entity : null;
            MySpectator spectator = MySpectator.Static;
            _savedState = new CameraState
            {
                Entity = entity,
                SpectatorMovement = spectator.SpectatorCameraMovement
            };

            if (preferControlledView)
                PreferControlledView(_savedState);
        }

        private static void PreferControlledView(CameraState state)
        {
            IMyControllableEntity controlled = MyAPIGateway.Session != null
                ? MyAPIGateway.Session.ControlledObject
                : null;
            MyEntity entity = controlled != null ? controlled.Entity as MyEntity : null;
            if (entity == null || entity.Closed || entity.MarkedForClose)
                return;

            state.Entity = entity;
        }

        private static MyEntity GetCurrentControlledEntity()
        {
            IMyControllableEntity controlled = MyAPIGateway.Session != null
                ? MyAPIGateway.Session.ControlledObject
                : null;
            return controlled != null ? controlled.Entity as MyEntity : null;
        }

        private static bool TrySetEntityFirstPerson(MyEntity entity)
        {
            if (entity == null || entity.Closed || entity.MarkedForClose ||
                MyAPIGateway.Session == null)
                return false;

            try
            {
                IMyCameraController entityCamera = entity as IMyCameraController;
                if (entityCamera != null)
                    entityCamera.IsInFirstPersonView = true;

                MyAPIGateway.Session.SetCameraController(MyCameraControllerEnum.Entity, entity);
                IMyCameraController activeCamera = MyAPIGateway.Session.CameraController;
                if (activeCamera != null)
                    activeCamera.IsInFirstPersonView = true;
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }
    }
}
