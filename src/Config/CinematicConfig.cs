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

        public MyKeys ToggleKey { get; private set; }
        public float ShotDurationSeconds { get; set; }
        public double Distance { get; set; }
        public double Height { get; set; }
        public double Side { get; set; }
        public double Smoothing { get; set; }
        public double LookAheadSeconds { get; set; }
        public bool Automatic { get; set; }
        public bool DebugEnabled { get; set; }

        public CinematicConfig()
        {
            ToggleKey = MyKeys.F9;
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
            Automatic = true;
            DebugEnabled = false;
        }
    }
}
