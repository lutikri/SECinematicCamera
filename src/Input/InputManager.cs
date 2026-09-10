using Sandbox.ModAPI;
using SECinematicCamera.Config;

namespace SECinematicCamera.Input
{
    public sealed class InputManager
    {
        private readonly CinematicConfig _config;

        public InputManager(CinematicConfig config)
        {
            _config = config;
        }

        public bool IsTogglePressed()
        {
            return CanHandleHotkeys() &&
                MyAPIGateway.Input.IsNewKeyPressed(_config.ToggleKey) && !IsControlPressed();
        }

        public bool IsCyclePressed()
        {
            return CanHandleHotkeys() &&
                MyAPIGateway.Input.IsNewKeyPressed(_config.CycleKey) && IsControlPressed();
        }

        public bool IsExitPressed()
        {
            return CanHandleHotkeys() &&
                MyAPIGateway.Input.IsNewKeyPressed(_config.ExitKey);
        }

        public bool IsSpacePressed()
        {
            return MyAPIGateway.Input != null && MyAPIGateway.Input.IsKeyPress(VRage.Input.MyKeys.Space);
        }

        private static bool IsControlPressed()
        {
            return MyAPIGateway.Input.IsKeyPress(VRage.Input.MyKeys.LeftControl) ||
                MyAPIGateway.Input.IsKeyPress(VRage.Input.MyKeys.RightControl);
        }

        private static bool CanHandleHotkeys()
        {
            return MyAPIGateway.Input != null &&
                (MyAPIGateway.Gui == null ||
                 (!MyAPIGateway.Gui.ChatEntryVisible && !MyAPIGateway.Gui.IsCursorVisible));
        }
    }
}
