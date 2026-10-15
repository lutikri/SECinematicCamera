using Sandbox.ModAPI;
using SECinematicCamera.Config;
using VRage.Game.ModAPI;
using VRage.Input;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace SECinematicCamera.Input
{
    public sealed class InputManager
    {
        private sealed class SuppressedControl
        {
            public IMyControl Control;
            public bool WasEnabled;
            public bool Captured;
        }

        private readonly CinematicConfig _config;
        private readonly SuppressedControl _toggle = new SuppressedControl();
        private readonly SuppressedControl _cycle = new SuppressedControl();
        private readonly SuppressedControl _action = new SuppressedControl();
        private readonly SuppressedControl _exit = new SuppressedControl();
        private static readonly MyStringId LookAroundControl = MyStringId.GetOrCompute("LOOKAROUND");
        private Vector2 _orbitMouseDelta;

        public InputManager(CinematicConfig config)
        {
            _config = config;
        }

        public bool IsTogglePressed()
        {
            return CanHandleShipHotkeys() && HasNoModifiers() && _toggle.Captured &&
                MyAPIGateway.Input.IsNewKeyPressed(_config.ToggleKey);
        }

        public bool IsActionPressed()
        {
            return CanHandleShipHotkeys() && HasNoModifiers() && _action.Captured &&
                MyAPIGateway.Input.IsNewKeyPressed(_config.ActionKey);
        }

        public bool IsCyclePressed()
        {
            return CanHandleShipHotkeys() && IsControlPressed() && !IsAltOrShiftPressed() &&
                _toggle.Captured && _cycle.Captured &&
                MyAPIGateway.Input.IsNewKeyPressed(_config.CycleKey);
        }

        public bool IsExitPressed()
        {
            return CanHandleHotkeys() && HasNoModifiers() && _exit.Captured &&
                MyAPIGateway.Input.IsNewKeyPressed(_config.ExitKey);
        }

        public void PrepareControlSuppression(bool cinematicActive)
        {
            // Reserve these controls before a key arrives, so vanilla F9/V cannot
            // switch the camera before we capture or restore the player's view.
            if (CanHandleShipHotkeys())
            {
                Suppress(_toggle, _config.ToggleKey, MyKeyboardModifiers.None);
                Suppress(_cycle, _config.CycleKey, MyKeyboardModifiers.Control);
                Suppress(_action, _config.ActionKey, MyKeyboardModifiers.None);
            }
            else
            {
                Restore(_action);
                Restore(_cycle);
                Restore(_toggle);
            }

            if (cinematicActive && CanHandleHotkeys())
                Suppress(_exit, _config.ExitKey, MyKeyboardModifiers.None);
            else
                Restore(_exit);
        }

        public void ReleaseSuppressedControls()
        {
            ClearOrbitInput();
            // Reverse capture order also handles GetControl returning the same
            // object for both the plain and modified F9 binding.
            System.Exception failure = null;
            try { Restore(_exit); }
            catch (System.Exception exception) { failure = exception; }
            try { Restore(_action); }
            catch (System.Exception exception) { failure = exception; }
            try { Restore(_cycle); }
            catch (System.Exception exception) { failure = exception; }
            try { Restore(_toggle); }
            catch (System.Exception exception) { failure = exception; }
            if (failure != null)
                throw failure;
        }

        public bool IsSpacePressed()
        {
            return MyAPIGateway.Input != null && MyAPIGateway.Input.IsKeyPress(MyKeys.Space);
        }

        public bool IsOrbitHeld()
        {
            // Use the native freelook binding so vanilla also withholds mouse
            // steering from the ship. Respect user remaps instead of forcing Alt.
            return CanHandleShipHotkeys() && !IsControlPressed() &&
                MyAPIGateway.Input.IsGameControlPressed(LookAroundControl);
        }

        public void CaptureOrbitInput()
        {
            if (IsOrbitHeld())
            {
                // Apply the gameplay settings in floating point: the ModAPI's
                // integer gameplay getters truncate slow subpixel motion.
                float sensitivity = MyAPIGateway.Input.GetMouseSensitivity();
                float x = MyAPIGateway.Input.GetMouseX() * sensitivity *
                    (MyAPIGateway.Input.GetMouseXInversion() ? -1f : 1f);
                float y = MyAPIGateway.Input.GetMouseY() * sensitivity *
                    (MyAPIGateway.Input.GetMouseYInversion() ? -1f : 1f);
                _orbitMouseDelta += new Vector2(x, y);
            }
        }

        public Vector2 ConsumeOrbitDelta()
        {
            Vector2 delta = _orbitMouseDelta;
            ClearOrbitInput();
            return delta;
        }

        public void ClearOrbitInput()
        {
            _orbitMouseDelta = Vector2.Zero;
        }

        private static bool HasNoModifiers()
        {
            return !IsControlPressed() && !IsAltOrShiftPressed();
        }

        private static bool IsControlPressed()
        {
            return MyAPIGateway.Input.IsKeyPress(MyKeys.LeftControl) ||
                MyAPIGateway.Input.IsKeyPress(MyKeys.RightControl);
        }

        private static bool IsAltOrShiftPressed()
        {
            return MyAPIGateway.Input.IsKeyPress(MyKeys.LeftAlt) ||
                MyAPIGateway.Input.IsKeyPress(MyKeys.RightAlt) ||
                MyAPIGateway.Input.IsKeyPress(MyKeys.LeftShift) ||
                MyAPIGateway.Input.IsKeyPress(MyKeys.RightShift);
        }

        private static void Suppress(SuppressedControl state, MyKeys key,
            MyKeyboardModifiers modifiers)
        {
            if (state.Captured)
                return;

            state.Control = MyAPIGateway.Input.GetControl(key, modifiers);
            state.WasEnabled = state.Control != null && state.Control.IsEnabled;
            state.Captured = true;
            if (state.Control != null)
                state.Control.IsEnabled = false;
        }

        private static void Restore(SuppressedControl state)
        {
            if (!state.Captured)
                return;
            try
            {
                if (state.Control != null)
                    state.Control.IsEnabled = state.WasEnabled;
            }
            finally
            {
                state.Control = null;
                state.Captured = false;
            }
        }

        private static bool CanHandleShipHotkeys()
        {
            if (!CanHandleHotkeys() || MyAPIGateway.Session == null ||
                MyAPIGateway.Session.LocalHumanPlayer == null ||
                (MyAPIGateway.CubeBuilder != null && MyAPIGateway.CubeBuilder.IsActivated))
                return false;

            var controlled = MyAPIGateway.Session.ControlledObject;
            var controller = controlled != null ? controlled.Entity as IMyShipController : null;
            return controller != null && !controller.Closed && !controller.MarkedForClose &&
                controller.CubeGrid != null && !controller.CubeGrid.Closed &&
                !controller.CubeGrid.MarkedForClose;
        }

        private static bool CanHandleHotkeys()
        {
            return MyAPIGateway.Input != null &&
                (MyAPIGateway.Gui == null ||
                 (!MyAPIGateway.Gui.ChatEntryVisible && !MyAPIGateway.Gui.IsCursorVisible));
        }
    }
}
