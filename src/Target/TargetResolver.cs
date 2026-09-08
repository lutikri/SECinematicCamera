using Sandbox.ModAPI;
using VRage.Game.ModAPI.Interfaces;

namespace SECinematicCamera.Target
{
    public sealed class TargetResolver
    {
        public bool TryResolve(out CinematicTarget target, out string reason)
        {
            target = null;
            reason = null;

            if (MyAPIGateway.Session == null || MyAPIGateway.Session.LocalHumanPlayer == null)
            {
                reason = "Local player is unavailable.";
                return false;
            }

            IMyControllableEntity controlled = MyAPIGateway.Session.ControlledObject;
            if (controlled == null || controlled.Entity == null)
            {
                reason = "No locally controlled entity.";
                return false;
            }

            IMyShipController controller = controlled.Entity as IMyShipController;
            if (controller == null || controller.CubeGrid == null)
            {
                reason = "Control a ship or rover cockpit/remote control first.";
                return false;
            }

            if (controller.Closed || controller.MarkedForClose || controller.CubeGrid.Closed || controller.CubeGrid.MarkedForClose)
            {
                reason = "The controlled ship or rover is invalid.";
                return false;
            }

            target = new CinematicTarget(controller, controller.CubeGrid);
            return true;
        }
    }
}
