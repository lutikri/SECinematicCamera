using Sandbox.ModAPI;
using SECinematicCamera.Camera;
using SECinematicCamera.Target;
using VRage.Game.ModAPI.Interfaces;

namespace SECinematicCamera.Safety
{
    public sealed class CinematicSafetyController
    {
        public bool TryValidate(CinematicTarget target, SECameraAdapter camera, out string reason)
        {
            reason = null;
            if (MyAPIGateway.Session == null || MyAPIGateway.Session.LocalHumanPlayer == null)
            {
                reason = "Local player became unavailable.";
                return false;
            }

            if (target == null || !target.IsValid)
            {
                reason = "Target grid or controller became invalid.";
                return false;
            }

            IMyControllableEntity controlled = MyAPIGateway.Session.ControlledObject;
            if (controlled == null || controlled.Entity == null)
            {
                reason = "Controlled entity became unavailable.";
                return false;
            }

            if (controlled.Entity.EntityId != target.ControlledEntityId)
            {
                reason = "Controlled entity changed unexpectedly.";
                return false;
            }

            if (target.Controller.CubeGrid == null ||
                target.Controller.CubeGrid.EntityId != target.GridEntityId)
            {
                reason = "Controlled grid changed unexpectedly.";
                return false;
            }

            if (!camera.HasExpectedCameraController())
            {
                reason = "Another system or user action took camera control.";
                return false;
            }

            return true;
        }
    }
}
