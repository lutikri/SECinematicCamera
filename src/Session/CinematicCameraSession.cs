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
        private bool _unloading;
        private int _startupRecoveryTicks;
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
            if (_camera.RecoverControlledCockpitFromSpectator())
                _logger.Info("Recovered cockpit view | FIRST PERSON");
            else if (!_camera.HasEntityCameraController())
                _startupRecoveryTicks = 180;
        }

        public override void UpdateAfterSimulation()
        {
            if (_unloading)
                return;

            if (_startupRecoveryTicks > 0)
            {
                _startupRecoveryTicks--;
                if (_camera.RecoverControlledCockpitFromSpectator())
                {
                    _startupRecoveryTicks = 0;
                    _logger.Info("Recovered cockpit view | FIRST PERSON");
                }
                else if (_camera.HasEntityCameraController())
                {
                    _startupRecoveryTicks = 0;
                }
            }

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
            _logger.Info("MANUAL | " + ShotProfile.GetShortName(_director.CurrentShot));
        }

        public void SetAutomatic(bool enabled)
        {
            _config.Automatic = enabled;
            _logger.Info(enabled ? "AUTO ON" : "AUTO OFF");
        }

        public void SetDebug(bool enabled)
        {
            _config.DebugEnabled = enabled;
            _logger.DebugEnabled = enabled;
            _debugReportSeconds = 0f;
            _spaceWasPressed = false;
            _spaceDebugSeconds = 0f;
            _logger.Info(enabled ? "DEBUG ON" : "DEBUG OFF");
        }

        public void ResetSettings()
        {
            _config.Reset();
            _logger.DebugEnabled = _config.DebugEnabled;
            _logger.Info("Defaults restored | AUTO ON");
        }

        public void ForceNextShot()
        {
            if (!_active)
            {
                _logger.Info("OFF | use F9 or /cine on");
                return;
            }

            _target.Refresh();
            if (_director.ForceNext(_target))
            {
                ApplyHardCut(_director.LastChangeReason);
                _logger.Info((_config.Automatic ? "AUTO" : "MANUAL") + " | " +
                    ShotProfile.GetShortName(_director.CurrentShot));
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
            _logger.Info("MANUAL | " + ShotProfile.GetShortName(shot));
        }

        public string GetStatus()
        {
            if (!_active || _target == null)
                return "OFF | next start: AUTO";

            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "ON | {0} | {1} | {2} | {3:0}/{4:0}s | {5:0}m/s",
                _config.Automatic ? "AUTO" : "MANUAL",
                ShotProfile.GetShortName(_director.CurrentShot),
                CompactName(_target.Grid.DisplayName), _director.ShotAgeSeconds,
                _director.CurrentDurationSeconds, _target.Speed);
        }

        public string GetSettings()
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "time {0:0.#}s | dist {1:0.##} | h {2:0.##} | side {3:0.##} | smooth {4:0.##} | lead {5:0.##}",
                _config.ShotDurationSeconds, _config.Distance, _config.Height, _config.Side,
                _config.Smoothing, _config.LookAheadSeconds);
        }

        public void EmergencyStop(string reason, bool forceControlledView = false,
            bool writeMessage = true)
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
                if (_logger != null && writeMessage)
                    _logger.Info("Camera restore failed: " + restoreException.Message);
            }

            try
            {
                if (_target != null)
                    _target.Clear();
            }
            catch (System.Exception) { }
            _target = null;

            try { if (_director != null) _director.Clear(); }
            catch (System.Exception) { }
            try { if (_smoother != null) _smoother.Clear(); }
            catch (System.Exception) { }
            try { if (_shotSolver != null) _shotSolver.Clear(); }
            catch (System.Exception) { }
            try { if (_camera != null) _camera.ClearTransientState(); }
            catch (System.Exception) { }

            if (_logger != null && writeMessage)
                _logger.Info("OFF | " + reason);
        }

        protected override void UnloadData()
        {
            _unloading = true;
            // Never call chat/UI services while the game is tearing the session
            // down. Alt+F4 can invalidate them before UnloadData finishes.
            try { EmergencyStop("Session/world unload.", true, false); }
            catch (System.Exception) { }
            try { if (_commands != null) _commands.Unregister(); }
            catch (System.Exception) { }
            try { base.UnloadData(); }
            catch (System.Exception) { }
        }

        private void TryStart(bool preferControlledView = false)
        {
            if (_active)
                return;

            // Manual testing is session-local. Every fresh activation starts in
            // a clearly announced automatic mode instead of silently preserving
            // Ctrl+F9 or /cine cam from the previous activation.
            _config.Automatic = true;

            string reason;
            CinematicTarget target;
            if (!_targetResolver.TryResolve(out target, out reason))
            {
                // F9 is also Space Engineers' vanilla spectator key. When the
                // player is on foot, vanilla may switch to free camera before
                // this mod rejects the missing ship target. Undo that camera
                // change only for the hotkey path; chat commands do not cause it.
                if (preferControlledView)
                    _camera.RestoreCurrentControlledFirstPerson();
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
            _logger.Info("ON | AUTO | Rear | " + CompactName(target.Grid.DisplayName));
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
            bool flyByTimedOut = _director.CurrentShot == ShotType.WorldLockedFlyBy &&
                _director.ShotAgeSeconds >= 6f;
            if (flyByTimedOut || _shotSolver.ShouldEndWorldLockedShot(_target, 700.0))
            {
                string flyByReason = flyByTimedOut
                    ? "Fly-by reached 6 second limit"
                    : "Fly-by ended: target receded 700 m from the fixed camera";
                if (_config.Automatic)
                {
                    changed = _director.RequestNext(_target, flyByReason);
                }
                else
                {
                    _director.RestartCurrent(flyByTimedOut
                        ? "Manual fly-by repositioned after 6 seconds"
                        : "Manual fly-by repositioned at 700 m");
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

        private static string CompactName(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "Grid";
            return value.Length <= 16 ? value : value.Substring(0, 15) + "…";
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
