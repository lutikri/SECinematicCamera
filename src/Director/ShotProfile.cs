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
        public double FocusForward { get; private set; }
        public double FocusUp { get; private set; }
        public double CockpitWeight { get; private set; }
        public double ScreenX { get; private set; }
        public double ScreenY { get; private set; }
        public bool UseLeadRoom { get; private set; }
        public double LeadRoomAmount { get; private set; }

        public ShotProfile(ShotType type, string name, double forwardDistance,
            double rightDistance, double heightScale, double sideBias, bool worldLocked,
            double focusForward, double focusUp, double cockpitWeight,
            double screenX, double screenY, bool useLeadRoom, double leadRoomAmount)
        {
            Type = type;
            Name = name;
            ForwardDistance = forwardDistance;
            RightDistance = rightDistance;
            HeightScale = heightScale;
            SideBias = sideBias;
            WorldLocked = worldLocked;
            FocusForward = focusForward;
            FocusUp = focusUp;
            CockpitWeight = cockpitWeight;
            ScreenX = screenX;
            ScreenY = screenY;
            UseLeadRoom = useLeadRoom;
            LeadRoomAmount = leadRoomAmount;
        }

        public static ShotProfile Get(ShotType type)
        {
            switch (type)
            {
                case ShotType.SideHighLeft:
                    return new ShotProfile(type, "Side High Left", -0.25, -0.90, 1.25, -0.35, false,
                        0.18, 0.08, 0.10, 0.0, -0.12, true, 0.22);
                case ShotType.SideHighRight:
                    return new ShotProfile(type, "Side High Right", -0.25, 0.90, 1.25, 0.35, false,
                        0.18, 0.08, 0.10, 0.0, -0.12, true, 0.22);
                case ShotType.FrontHighThreeQuarter:
                    return new ShotProfile(type, "Front High Three-Quarter", 1.0, 0.0, 1.1, -0.65, false,
                        0.22, 0.10, 0.25, 0.18, -0.15, false, 0.0);
                case ShotType.OverheadDrift:
                    return new ShotProfile(type, "Overhead Drift", -0.15, 0.0, 2.4, 0.0, false,
                        0.08, 0.0, 0.05, 0.0, -0.06, false, 0.0);
                case ShotType.WorldLockedFlyBy:
                    return new ShotProfile(type, "World-Locked Fly-By", 1.30, 0.45, 1.35, 0.0, true,
                        0.12, 0.08, 0.10, 0.0, -0.10, true, 0.25);
                default:
                    return new ShotProfile(ShotType.RearHighThreeQuarter, "Rear High Three-Quarter",
                        -1.0, 0.0, 1.0, 0.65, false,
                        0.25, 0.08, 0.15, -0.18, -0.16, false, 0.0);
            }
        }
    }
}
