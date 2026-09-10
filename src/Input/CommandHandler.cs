using Sandbox.ModAPI;
using SECinematicCamera.Config;
using SECinematicCamera.Debug;
using SECinematicCamera.Director;
using SECinematicCamera.Session;
using System;
using System.Globalization;

namespace SECinematicCamera.Input
{
    public sealed class CommandHandler
    {
        private readonly CinematicCameraSession _session;
        private readonly CinematicConfig _config;
        private readonly CineLogger _logger;
        private bool _registered;

        public CommandHandler(CinematicCameraSession session, CinematicConfig config, CineLogger logger)
        {
            _session = session;
            _config = config;
            _logger = logger;
        }

        public void Register()
        {
            if (_registered || MyAPIGateway.Utilities == null)
                return;

            MyAPIGateway.Utilities.MessageEnteredSender += OnMessageEntered;
            _registered = true;
        }

        public void Unregister()
        {
            if (!_registered || MyAPIGateway.Utilities == null)
                return;

            MyAPIGateway.Utilities.MessageEnteredSender -= OnMessageEntered;
            _registered = false;
        }

        private void OnMessageEntered(ulong sender, string messageText, ref bool sendToOthers)
        {
            if (string.IsNullOrWhiteSpace(messageText))
                return;

            string[] args = messageText.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (args.Length == 0 || !args[0].Equals("/cine", StringComparison.OrdinalIgnoreCase))
                return;

            sendToOthers = false;
            try
            {
                Execute(args);
            }
            catch (Exception exception)
            {
                _session.EmergencyStop("Command exception: " + exception.Message);
            }
        }

        private void Execute(string[] args)
        {
            if (args.Length == 1)
            {
                _session.ToggleCinematic();
                return;
            }

            string command = args[1].ToLowerInvariant();
            switch (command)
            {
                case "on":
                    _session.SetCinematicEnabled(true);
                    return;
                case "off":
                    _session.SetCinematicEnabled(false);
                    return;
                case "next":
                    _session.ForceNextShot();
                    return;
                case "auto":
                    SetBoolean(args, "auto", value => _session.SetAutomatic(value));
                    return;
                case "cam":
                    SelectCamera(args);
                    return;
                case "set":
                    SetParameter(args);
                    return;
                case "status":
                    _logger.Info(_session.GetStatus());
                    return;
                case "get":
                    _logger.Info(_session.GetSettings());
                    return;
                case "reset":
                    _session.ResetSettings();
                    return;
                case "debug":
                    SetBoolean(args, "debug", value => _session.SetDebug(value));
                    return;
                case "help":
                    ShowHelp();
                    return;
                default:
                    _logger.Info("Unknown command. Use /cine help.");
                    return;
            }
        }

        private void SelectCamera(string[] args)
        {
            ShotType shot;
            if (args.Length != 3 || !TryParseShot(args[2], out shot))
            {
                _logger.Info("Usage: /cine cam <rear|sideleft|sideright|front|overhead|flyby|frontmount|rearmount|distant|1..9>");
                return;
            }

            _session.SelectManualShot(shot);
        }

        private void SetParameter(string[] args)
        {
            double value;
            if (args.Length != 4 || !double.TryParse(args[3], NumberStyles.Float,
                CultureInfo.InvariantCulture, out value))
            {
                ShowSetUsage();
                return;
            }

            string name = args[2].ToLowerInvariant();
            bool valid;
            switch (name)
            {
                case "duration":
                    valid = value >= 1.0 && value <= 60.0;
                    if (valid) _config.ShotDurationSeconds = (float)value;
                    break;
                case "distance":
                    valid = value >= 0.1 && value <= 20.0;
                    if (valid) _config.Distance = value;
                    break;
                case "height":
                    valid = value >= 0.1 && value <= 10.0;
                    if (valid) _config.Height = value;
                    break;
                case "side":
                    valid = value >= 0.0 && value <= 10.0;
                    if (valid) _config.Side = value;
                    break;
                case "smoothing":
                    valid = value >= 0.01 && value <= 1.0;
                    if (valid) _config.Smoothing = value;
                    break;
                case "lookahead":
                    valid = value >= 0.0 && value <= 5.0;
                    if (valid) _config.LookAheadSeconds = value;
                    break;
                default:
                    ShowSetUsage();
                    return;
            }

            if (!valid)
            {
                _logger.Info("Value is outside the safe range. Use /cine help.");
                return;
            }

            _logger.Info(name + " = " + value.ToString("0.###", CultureInfo.InvariantCulture));
        }

        private void SetBoolean(string[] args, string name, Action<bool> setter)
        {
            if (args.Length != 3 ||
                (!args[2].Equals("on", StringComparison.OrdinalIgnoreCase) &&
                 !args[2].Equals("off", StringComparison.OrdinalIgnoreCase)))
            {
                _logger.Info("Usage: /cine " + name + " on|off");
                return;
            }

            setter(args[2].Equals("on", StringComparison.OrdinalIgnoreCase));
        }

        private static bool TryParseShot(string value, out ShotType shot)
        {
            switch (value.ToLowerInvariant())
            {
                case "rear": case "1": shot = ShotType.RearHighThreeQuarter; return true;
                case "sideleft": case "2": shot = ShotType.SideHighLeft; return true;
                case "sideright": case "3": shot = ShotType.SideHighRight; return true;
                case "front": case "4": shot = ShotType.FrontHighThreeQuarter; return true;
                case "overhead": case "5": shot = ShotType.OverheadDrift; return true;
                case "flyby": case "6": shot = ShotType.WorldLockedFlyBy; return true;
                case "frontmount": case "goprofront": case "7": shot = ShotType.FrontMountedClose; return true;
                case "rearmount": case "goprorear": case "8": shot = ShotType.RearMountedForward; return true;
                case "distant": case "topside": case "9": shot = ShotType.DistantTopSide; return true;
                default: shot = ShotType.RearHighThreeQuarter; return false;
            }
        }

        private void ShowSetUsage()
        {
            _logger.Info("Usage: /cine set <duration|distance|height|side|smoothing|lookahead> <value>");
        }

        private void ShowHelp()
        {
            _logger.Info("/cine [on|off|next|status|get|reset|help], auto on|off, debug on|off");
            _logger.Info("/cine cam <rear|sideleft|sideright|front|overhead|flyby|frontmount|rearmount|distant|1..9>");
            ShowSetUsage();
        }
    }
}
