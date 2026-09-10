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
        private ScreenComposer _screenComposer;
        private CameraSmoother _smoother;
        private CameraShake _shake;
        private SECameraAdapter _camera;
        private CinematicSafetyController _safety;
        private CinematicTarget _target;
        private bool _active;
        private float _debugReportSeconds;
        private bool _spaceWasPressed;
        private float _spaceDebugSeconds;
        private VRageMath.Vector3D _spaceStartCameraPosition;
        private VRageMath.Vector3D _spaceStartGridPosition;

        public override void BeforeStart()
        {
            _config = new CinematicConfig();
            _logger = new CineLogger();
            _input = new InputManager(_config);
            _commands = new CommandHandler(this, _config, _logger);
            _targetResolver = new TargetResolver();
            _director = new CameraDirector(_config);
            _shotSolver = new ShotSolver(_config);
            _screenComposer = new ScreenComposer();
            _smoother = new CameraSmoother();
            _shake = new CameraShake();
            _camera = new SECameraAdapter();
            _safety = new CinematicSafetyController();
            _commands.Register();
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                if (_input != null && _active && _input.IsExitPressed())
                {
                    EmergencyStop("Vanilla view requested with V.", true);
                    return;
                }

                if (_input != null && _input.IsCyclePressed())
                {
                    CycleManualShot();
                }
                else if (_input != null && _input.IsTogglePressed())
                {
                    if (_active)
                        EmergencyStop("Cinematic mode disabled.");
                    else
                        TryStart(true);
                }

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

        public void ToggleCinematic()
        {
            SetCinematicEnabled(!_active);
        }

        public void CycleManualShot()
        {
            if (!_active)
            {
                TryStart(true);
                return;
            }

            _config.Automatic = false;
            _director.SelectNextManual();
            ApplyHardCut(_director.LastChangeReason);
            _logger.Info("Camera: " + ShotProfile.Get(_director.CurrentShot).Name + "; auto OFF.");
        }

        public void SetAutomatic(bool enabled)
        {
            _config.Automatic = enabled;
            _logger.Info("Automatic shots " + (enabled ? "ON." : "OFF."));
        }

        public void SetDebug(bool enabled)
        {
            _config.DebugEnabled = enabled;
            _logger.DebugEnabled = enabled;
            _debugReportSeconds = 0f;
            _spaceWasPressed = false;
            _spaceDebugSeconds = 0f;
            _logger.Info("Debug " + (enabled ? "ON." : "OFF."));
        }

        public void ResetSettings()
        {
            _config.Reset();
            _logger.DebugEnabled = _config.DebugEnabled;
            _logger.Info("Runtime settings reset to compiled defaults.");
        }

        public void ForceNextShot()
        {
            if (!_active)
            {
                _logger.Info("Cinematic mode is OFF.");
                return;
            }

            _target.Refresh();
            if (_director.ForceNext(_target))
            {
                ApplyHardCut(_director.LastChangeReason);
                _logger.Info("Camera: " + ShotProfile.Get(_director.CurrentShot).Name + ".");
            }
        }

        public void SelectManualShot(ShotType shot)
        {
            if (!_active)
                TryStart();
            if (!_active)
                return;

            _config.Automatic = false;
            _director.SelectManual(shot);
            ApplyHardCut(_director.LastChangeReason);
            _logger.Info("Camera: " + ShotProfile.Get(shot).Name + "; auto OFF.");
        }

        public string GetStatus()
        {
            if (!_active || _target == null)
                return "OFF; auto " + (_config.Automatic ? "ON" : "OFF") + ".";

            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "ON; target {0}; shot {1}; age {2:0.0}/{3:0.0}s; speed {4:0.0} m/s; {5}; {6}; {7}",
                _target.Grid.DisplayName, _director.CurrentShot, _director.ShotAgeSeconds,
                _director.CurrentDurationSeconds, _target.Speed, _director.LastChangeReason,
                _screenComposer.GetDiagnostics(), _smoother.GetDiagnostics());
        }

        public string GetSettings()
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "duration {0:0.###}, distance {1:0.###}, height {2:0.###}, side {3:0.###}, smoothing {4:0.###}, lookahead {5:0.###}",
                _config.ShotDurationSeconds, _config.Distance, _config.Height, _config.Side,
                _config.Smoothing, _config.LookAheadSeconds);
        }

        public void EmergencyStop(string reason, bool forceControlledView = false)
        {
            _active = false;
            _debugReportSeconds = 0f;
            _spaceWasPressed = false;
            _spaceDebugSeconds = 0f;

            try
            {
                if (_camera != null)
                    _camera.Restore(forceControlledView);
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
            if (_shotSolver != null)
                _shotSolver.Clear();
            if (_camera != null)
                _camera.ClearTransientState();

            if (_logger != null)
                _logger.Info("OFF — " + reason);
        }

        protected override void UnloadData()
        {
            if (_commands != null)
                _commands.Unregister();
            EmergencyStop("Session/world unload.");
            base.UnloadData();
        }

        private void TryStart(bool preferControlledView = false)
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
            _shotSolver.BeginShot(target, _director.CurrentShot);
            CameraPose initialPose = SolveComposedPose(target, _director.CurrentShot);
            initialPose = _smoother.Reset(initialPose, target.Position);
            if (!_camera.TryEnterFixed(initialPose, preferControlledView, out reason))
            {
                target.Clear();
                _director.Clear();
                _smoother.Clear();
                _shotSolver.Clear();
                _camera.ClearTransientState();
                _logger.Info(reason);
                return;
            }

            _target = target;
            _active = true;
            _debugReportSeconds = 0f;
            _spaceWasPressed = false;
            _spaceDebugSeconds = 0f;
            _logger.Info("ON — RearHighThreeQuarter; target " + target.Grid.DisplayName + ".");
            _logger.Debug("Spectator movement before activation: " + _camera.CapturedMovementName +
                "; active: " + _camera.GetLastWriteDiagnostics().SpectatorMovement + ".");
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
            bool changed;
            if (_shotSolver.ShouldEndWorldLockedShot(_target, 700.0))
            {
                if (_config.Automatic)
                {
                    changed = _director.RequestNext(_target,
                        "Fly-by ended: target receded 700 m from the fixed camera");
                }
                else
                {
                    _director.RestartCurrent("Manual fly-by repositioned at 700 m");
                    ApplyHardCut(_director.LastChangeReason);
                    return;
                }
            }
            else
            {
                changed = _director.Update(_target, TickSeconds);
            }
            CameraPose desired = SolveComposedPose(_target, _director.CurrentShot);
            CameraPose pose;
            if (changed)
            {
                _shotSolver.BeginShot(_target, _director.CurrentShot);
                desired = SolveComposedPose(_target, _director.CurrentShot);
                pose = _smoother.Reset(desired, _target.Position);
                _logger.Debug("CUT to " + ShotProfile.Get(_director.CurrentShot).Name +
                    " — " + _director.LastChangeReason + ".");
            }
            else
            {
                pose = _smoother.Step(desired, _target.Position, _target.LinearVelocity,
                    TickSeconds, _config.Smoothing, ShotProfile.Get(_director.CurrentShot),
                    _target.Scale);
            }
            CameraPose finalPose = _shake.Apply(pose, _target,
                ShotProfile.Get(_director.CurrentShot), _director.ShotAgeSeconds);
            _camera.ApplyPose(finalPose);
            UpdateSpectatorInputDiagnostics();

            if (_config.DebugEnabled)
            {
                _debugReportSeconds += TickSeconds;
                if (_debugReportSeconds >= 5f)
                {
                    _debugReportSeconds = 0f;
                    CameraWriteDiagnostics diagnostics = _camera.GetLastWriteDiagnostics();
                    _logger.Debug(GetStatus() + "; spectator movement " +
                        diagnostics.SpectatorMovement + "; pose rewritten " +
                        diagnostics.PoseRewrittenThisTick + ".");
                }
            }
        }

        private void UpdateSpectatorInputDiagnostics()
        {
            if (!_config.DebugEnabled)
                return;

            bool spacePressed = _input.IsSpacePressed();
            CameraWriteDiagnostics diagnostics = _camera.GetLastWriteDiagnostics();
            if (spacePressed && !_spaceWasPressed)
            {
                _spaceStartCameraPosition = diagnostics.PositionBeforeWrite;
                _spaceStartGridPosition = _target.Position;
                _spaceDebugSeconds = 0f;
                LogSpaceDiagnostics("Space DOWN", diagnostics);
            }
            else if (spacePressed)
            {
                _spaceDebugSeconds += TickSeconds;
                if (_spaceDebugSeconds >= 1f)
                {
                    _spaceDebugSeconds = 0f;
                    LogSpaceDiagnostics("Space HELD", diagnostics);
                }
            }
            else if (_spaceWasPressed)
            {
                LogSpaceDiagnostics("Space UP", diagnostics);
                _logger.Debug("Space interval delta — camera " +
                    FormatVector(diagnostics.PositionAfterWrite - _spaceStartCameraPosition) +
                    "; grid " + FormatVector(_target.Position - _spaceStartGridPosition) + ".");
            }

            _spaceWasPressed = spacePressed;
        }

        private void LogSpaceDiagnostics(string phase, CameraWriteDiagnostics diagnostics)
        {
            _logger.Debug(phase + " — movement " + diagnostics.SpectatorMovement +
                "; camera before " + FormatVector(diagnostics.PositionBeforeWrite) +
                "; after " + FormatVector(diagnostics.PositionAfterWrite) +
                "; grid " + FormatVector(_target.Position) +
                "; pose rewritten " + diagnostics.PoseRewrittenThisTick + ".");
        }

        private static string FormatVector(VRageMath.Vector3D value)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "({0:0.00}, {1:0.00}, {2:0.00})", value.X, value.Y, value.Z);
        }

        private void ApplyHardCut(string reason)
        {
            _target.Refresh();
            _shotSolver.BeginShot(_target, _director.CurrentShot);
            CameraPose pose = _smoother.Reset(SolveComposedPose(_target, _director.CurrentShot),
                _target.Position);
            pose = _shake.Apply(pose, _target, ShotProfile.Get(_director.CurrentShot), 0.0);
            _camera.ApplyPose(pose);
            _logger.Debug("CUT to " + ShotProfile.Get(_director.CurrentShot).Name + " — " + reason + ".");
        }

        private CameraPose SolveComposedPose(CinematicTarget target, ShotType shot)
        {
            CameraPose basePose = _shotSolver.Solve(target, shot);
            return _screenComposer.Compose(basePose, target, ShotProfile.Get(shot),
                _camera.GetProjectionInfo());
        }
    }
}
