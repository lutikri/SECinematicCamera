using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Interfaces;
using VRageMath;

namespace SECinematicCamera.Camera
{
    public sealed class SECameraAdapter
    {
        private sealed class CameraState
        {
            public IMyCameraController Controller;
            public MyEntity Entity;
            public bool IsInFirstPerson;
            public bool IsSpectator;
            public MySpectatorCameraMovementEnum SpectatorMovement;
            public Vector3D SpectatorPosition;
            public Vector3D SpectatorTarget;
        }

        private CameraState _savedState;
        private IMyCameraController _expectedController;

        public bool HasCapturedState { get { return _savedState != null; } }

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
                MyAPIGateway.Session.SetCameraController(MyCameraControllerEnum.SpectatorFixed, null, initialPose.Position);
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
                   object.ReferenceEquals(MyAPIGateway.Session.CameraController, _expectedController);
        }

        public void ApplyPose(CameraPose pose)
        {
            if (MySpectator.Static == null)
                throw new System.InvalidOperationException("Spectator camera became unavailable.");

            MySpectator.Static.Position = pose.Position;
            MySpectator.Static.SetTarget(pose.LookAt, pose.Up);
        }

        public void Restore()
        {
            CameraState state = _savedState;
            _savedState = null;
            _expectedController = null;
            if (state == null || MyAPIGateway.Session == null)
                return;

            if (!state.IsSpectator && state.Entity != null && !state.Entity.Closed && !state.Entity.MarkedForClose)
            {
                MyCameraControllerEnum mode = state.IsInFirstPerson
                    ? MyCameraControllerEnum.Entity
                    : MyCameraControllerEnum.ThirdPersonSpectator;
                MyAPIGateway.Session.SetCameraController(mode, state.Entity);
                if (MyAPIGateway.Session.CameraController != null)
                    MyAPIGateway.Session.CameraController.IsInFirstPersonView = state.IsInFirstPerson;
                return;
            }

            if (state.IsSpectator && MySpectator.Static != null)
            {
                MyAPIGateway.Session.SetCameraController(ToCameraMode(state.SpectatorMovement), null, state.SpectatorPosition);
                MySpectator.Static.Position = state.SpectatorPosition;
                MySpectator.Static.SetTarget(state.SpectatorTarget, null);
                return;
            }

            IMyControllableEntity controlled = MyAPIGateway.Session.ControlledObject;
            MyAPIGateway.Session.SetCameraController(MyCameraControllerEnum.Entity,
                controlled != null ? controlled.Entity : null);
        }

        public void ClearTransientState()
        {
            _expectedController = null;
        }

        private void CaptureCurrentState()
        {
            IMyCameraController controller = MyAPIGateway.Session.CameraController;
            MyEntity entity = controller != null ? controller.Entity : null;
            MySpectator spectator = MySpectator.Static;
            _savedState = new CameraState
            {
                Controller = controller,
                Entity = entity,
                IsInFirstPerson = controller != null && controller.IsInFirstPersonView,
                IsSpectator = entity == null,
                SpectatorMovement = spectator.SpectatorCameraMovement,
                SpectatorPosition = spectator.Position,
                SpectatorTarget = spectator.Target
            };
        }

        private static MyCameraControllerEnum ToCameraMode(MySpectatorCameraMovementEnum movement)
        {
            switch (movement)
            {
                case MySpectatorCameraMovementEnum.None:
                    return MyCameraControllerEnum.SpectatorFixed;
                case MySpectatorCameraMovementEnum.ConstantDelta:
                    return MyCameraControllerEnum.SpectatorDelta;
                case MySpectatorCameraMovementEnum.FreeMouse:
                    return MyCameraControllerEnum.SpectatorFreeMouse;
                case MySpectatorCameraMovementEnum.Orbit:
                    return MyCameraControllerEnum.SpectatorOrbit;
                default:
                    return MyCameraControllerEnum.Spectator;
            }
        }
    }
}
