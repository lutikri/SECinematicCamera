using Sandbox.ModAPI;
using SECinematicCamera.Camera;
using SECinematicCamera.Config;
using SECinematicCamera.Debug;
using SECinematicCamera.Director;
using SECinematicCamera.Input;
using SECinematicCamera.Safety;
using SECinematicCamera.Target;
using VRage.Game.Components;

namespace SECinematicCamera.Session
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public sealed class CinematicCameraSession : MySessionComponentBase
    {
        private const float TickSeconds = 1f / 60f;

        private CinematicConfig _config;
        private CineLogger _logger;
        private InputManager _input;
        private CommandHandler _commands;
        private TargetResolver _targetResolver;
        private CameraDirector _director;
        private ShotSolver _shotSolver;
        private CameraSmoother _smoother;
        private SECameraAdapter _camera;
        private CinematicSafetyController _safety;
        private CinematicTarget _target;
        private bool _active;

        public override void BeforeStart()
        {
            _config = new CinematicConfig();
            _logger = new CineLogger();
            _input = new InputManager(_config);
            _commands = new CommandHandler();
            _targetResolver = new TargetResolver();
            _director = new CameraDirector();
            _shotSolver = new ShotSolver(_config);
            _smoother = new CameraSmoother();
            _camera = new SECameraAdapter();
            _safety = new CinematicSafetyController();
            _commands.Register();
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                if (_input != null && _input.IsTogglePressed())
                    SetCinematicEnabled(!_active);

                if (_active)
                    UpdateCinematic();
            }
            catch (System.Exception exception)
            {
                EmergencyStop("Cinematic update exception: " + exception.Message);
            }
        }

        public void SetCinematicEnabled(bool enabled)
        {
            if (enabled)
                TryStart();
            else
                EmergencyStop("Cinematic mode disabled.");
        }

        public void EmergencyStop(string reason)
        {
            bool hadState = _active || (_camera != null && _camera.HasCapturedState);
            _active = false;

            try
            {
                if (_camera != null)
                    _camera.Restore();
            }
            catch (System.Exception restoreException)
            {
                if (_logger != null)
                    _logger.Info("Camera restore failed: " + restoreException.Message);
            }

            if (_target != null)
                _target.Clear();
            _target = null;

            if (_director != null)
                _director.Clear();
            if (_smoother != null)
                _smoother.Clear();
            if (_camera != null)
                _camera.ClearTransientState();

            if (hadState && _logger != null)
                _logger.Info("OFF — " + reason);
        }

        protected override void UnloadData()
        {
            if (_commands != null)
                _commands.Unregister();
            EmergencyStop("Session/world unload.");
            base.UnloadData();
        }

        private void TryStart()
        {
            if (_active)
                return;

            string reason;
            CinematicTarget target;
            if (!_targetResolver.TryResolve(out target, out reason))
            {
                _logger.Info(reason);
                return;
            }

            target.Refresh();
            _director.Reset(target);
            CameraPose initialPose = _shotSolver.Solve(target, _director.CurrentShot);
            initialPose = _smoother.Reset(initialPose);
            if (!_camera.TryEnterFixed(initialPose, out reason))
            {
                target.Clear();
                _director.Clear();
                _smoother.Clear();
                _logger.Info(reason);
                return;
            }

            _target = target;
            _active = true;
            _logger.Info("ON — RearHighThreeQuarter; target " + target.Grid.DisplayName + ".");
        }

        private void UpdateCinematic()
        {
            string reason;
            if (!_safety.TryValidate(_target, _camera, out reason))
            {
                EmergencyStop(reason);
                return;
            }

            _target.Refresh();
            bool changed = _director.Update(_target, TickSeconds);
            CameraPose desired = _shotSolver.Solve(_target, _director.CurrentShot);
            CameraPose pose = changed ? _smoother.Reset(desired) : _smoother.Step(desired, _config.Smoothing);
            _camera.ApplyPose(pose);
        }
    }
}
