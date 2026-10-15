using VRage.Input;

namespace SECinematicCamera.Config
{
    public sealed class CinematicConfig
    {
        public const float DefaultShotDurationSeconds = 10f;
        public const double DefaultDistance = 0.75;
        public const double DefaultHeight = 1.2;
        public const double DefaultSide = 1.1;
        public const double DefaultSmoothing = 0.16;
        public const double DefaultLookAheadSeconds = 0.05;
        public const double DefaultOrbitSensitivity = 0.03;

        public MyKeys ToggleKey { get; private set; }
        public MyKeys ActionKey { get; private set; }
        public MyKeys ExitKey { get; private set; }
        public MyKeys CycleKey { get; private set; }
        public float ShotDurationSeconds { get; set; }
        public double Distance { get; set; }
        public double Height { get; set; }
        public double Side { get; set; }
        public double Smoothing { get; set; }
        public double LookAheadSeconds { get; set; }
        public double OrbitSensitivity { get; set; }
        public bool OrbitReturnEnabled { get; set; }
        public bool Automatic { get; set; }
        public bool DebugEnabled { get; set; }

        public CinematicConfig()
        {
            ToggleKey = MyKeys.F9;
            ActionKey = MyKeys.T;
            ExitKey = MyKeys.V;
            CycleKey = MyKeys.F9;
            Reset();
        }

        public void Reset()
        {
            ShotDurationSeconds = DefaultShotDurationSeconds;
            Distance = DefaultDistance;
            Height = DefaultHeight;
            Side = DefaultSide;
            Smoothing = DefaultSmoothing;
            LookAheadSeconds = DefaultLookAheadSeconds;
            OrbitSensitivity = DefaultOrbitSensitivity;
            OrbitReturnEnabled = true;
            Automatic = true;
            DebugEnabled = false;
        }
    }
}
