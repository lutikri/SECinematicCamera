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
            return MyAPIGateway.Input != null && MyAPIGateway.Input.IsNewKeyPressed(_config.ToggleKey);
        }
    }
}
