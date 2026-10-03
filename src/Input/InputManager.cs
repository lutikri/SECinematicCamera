using Sandbox.ModAPI;
using SECinematicCamera.Config;
using VRage.Input;
using VRage.ModAPI;

namespace SECinematicCamera.Input
{
    public sealed class InputManager
    {
        private readonly CinematicConfig _config;
        private IMyControl _alternateToggleControl;
        private IMyControl _alternateCycleControl;
        private bool _alternateToggleWasEnabled;
        private bool _alternateCycleWasEnabled;
        private bool _alternateToggleSuppressed;
        private bool _alternateCycleSuppressed;
        private bool _alternateToggleProtected;
        private bool _alternateCycleProtected;

        public InputManager(CinematicConfig config)
        {
            _config = config;
        }

        public bool IsTogglePressed()
        {
            if (!CanHandleHotkeys() || IsControlPressed())
                return false;

            return MyAPIGateway.Input.IsNewKeyPressed(_config.ToggleKey) ||
                (_alternateToggleProtected &&
                 MyAPIGateway.Input.IsNewKeyPressed(_config.AlternateToggleKey));
        }

        public bool IsCyclePressed()
        {
            if (!CanHandleHotkeys() || !IsControlPressed())
                return false;

            return MyAPIGateway.Input.IsNewKeyPressed(_config.CycleKey) ||
                (_alternateCycleProtected &&
                 MyAPIGateway.Input.IsNewKeyPressed(_config.AlternateToggleKey));
        }

        public bool IsExitPressed()
        {
            return CanHandleHotkeys() &&
                MyAPIGateway.Input.IsNewKeyPressed(_config.ExitKey);
        }

        public void PrepareControlSuppression(bool cinematicActive)
        {
            _alternateToggleProtected = false;
            _alternateCycleProtected = false;
            if (MyAPIGateway.Input == null)
                return;

            if (cinematicActive)
            {
                _alternateToggleProtected = SuppressControl(_config.AlternateToggleKey,
                    MyKeyboardModifiers.None, ref _alternateToggleControl,
                    ref _alternateToggleWasEnabled, ref _alternateToggleSuppressed);
                _alternateCycleProtected = SuppressControl(_config.AlternateToggleKey,
                    MyKeyboardModifiers.Control, ref _alternateCycleControl,
                    ref _alternateCycleWasEnabled, ref _alternateCycleSuppressed);
                return;
            }

            if (!CanHandleHotkeys() ||
                !MyAPIGateway.Input.IsNewKeyPressed(_config.AlternateToggleKey))
                return;

            if (IsControlPressed())
            {
                _alternateCycleProtected = SuppressControl(_config.AlternateToggleKey,
                    MyKeyboardModifiers.Control, ref _alternateCycleControl,
                    ref _alternateCycleWasEnabled, ref _alternateCycleSuppressed);
            }
            else
            {
                _alternateToggleProtected = SuppressControl(_config.AlternateToggleKey,
                    MyKeyboardModifiers.None, ref _alternateToggleControl,
                    ref _alternateToggleWasEnabled, ref _alternateToggleSuppressed);
            }
        }

        public void ReleaseSuppressedControls()
        {
            bool sharedAlternateControl = _alternateToggleControl != null &&
                object.ReferenceEquals(_alternateToggleControl, _alternateCycleControl);
            RestoreControl(ref _alternateToggleControl, _alternateToggleWasEnabled,
                ref _alternateToggleSuppressed);
            if (sharedAlternateControl)
            {
                _alternateCycleControl = null;
                _alternateCycleSuppressed = false;
            }
            else
            {
                RestoreControl(ref _alternateCycleControl, _alternateCycleWasEnabled,
                    ref _alternateCycleSuppressed);
            }
            _alternateToggleProtected = false;
            _alternateCycleProtected = false;
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

        private static bool SuppressControl(MyKeys key, MyKeyboardModifiers modifiers,
            ref IMyControl control, ref bool wasEnabled, ref bool suppressed)
        {
            if (suppressed)
                return true;

            try
            {
                control = MyAPIGateway.Input.GetControl(key, modifiers);
                if (control == null)
                {
                    suppressed = true;
                    return true;
                }
                wasEnabled = control.IsEnabled;
                control.IsEnabled = false;
                suppressed = true;
                return true;
            }
            catch (System.Exception)
            {
                control = null;
                suppressed = false;
                return false;
            }
        }

        private static void RestoreControl(ref IMyControl control, bool wasEnabled,
            ref bool suppressed)
        {
            if (!suppressed)
                return;

            try
            {
                if (control != null)
                    control.IsEnabled = wasEnabled;
            }
            catch (System.Exception) { }
            control = null;
            suppressed = false;
        }

        private static bool CanHandleHotkeys()
        {
            return MyAPIGateway.Input != null &&
                (MyAPIGateway.Gui == null ||
                 (!MyAPIGateway.Gui.ChatEntryVisible && !MyAPIGateway.Gui.IsCursorVisible));
        }
    }
}
