namespace SECinematicCamera.Director
{
    public sealed class ShotProfile
    {
        public ShotType Type { get; private set; }
        public string Name { get; private set; }
        public double ForwardDistance { get; private set; }
        public double RightDistance { get; private set; }
        public double HeightScale { get; private set; }
        public double SideBias { get; private set; }
        public bool WorldLocked { get; private set; }

        public ShotProfile(ShotType type, string name, double forwardDistance,
            double rightDistance, double heightScale, double sideBias, bool worldLocked)
        {
            Type = type;
            Name = name;
            ForwardDistance = forwardDistance;
            RightDistance = rightDistance;
            HeightScale = heightScale;
            SideBias = sideBias;
            WorldLocked = worldLocked;
        }

        public static ShotProfile Get(ShotType type)
        {
            switch (type)
            {
                case ShotType.SideHighLeft:
                    return new ShotProfile(type, "Side High Left", -0.25, -0.90, 1.25, -0.35, false);
                case ShotType.SideHighRight:
                    return new ShotProfile(type, "Side High Right", -0.25, 0.90, 1.25, 0.35, false);
                case ShotType.FrontHighThreeQuarter:
                    return new ShotProfile(type, "Front High Three-Quarter", 1.0, 0.0, 1.1, -0.65, false);
                case ShotType.OverheadDrift:
                    return new ShotProfile(type, "Overhead Drift", -0.15, 0.0, 2.4, 0.0, false);
                case ShotType.WorldLockedFlyBy:
                    return new ShotProfile(type, "World-Locked Fly-By", 1.30, 0.45, 1.35, 0.0, true);
                default:
                    return new ShotProfile(ShotType.RearHighThreeQuarter, "Rear High Three-Quarter",
                        -1.0, 0.0, 1.0, 0.65, false);
            }
        }
    }
}
