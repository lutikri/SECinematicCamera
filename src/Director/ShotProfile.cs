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
        public bool KeepBoundsInFrame { get; private set; }
        public double DurationScale { get; private set; }
        public double PositionDampingSeconds { get; private set; }
        public double AimDampingSeconds { get; private set; }
        public double AccelerationLagSeconds { get; private set; }
        public double MaximumLagScale { get; private set; }
        public double ShakePositionScale { get; private set; }
        public double ShakeAngleDegrees { get; private set; }
        public double ShakeFrequency { get; private set; }

        public ShotProfile(ShotType type, string name, double forwardDistance,
            double rightDistance, double heightScale, double sideBias, bool worldLocked,
            double focusForward, double focusUp, double cockpitWeight,
            double screenX, double screenY, bool useLeadRoom, double leadRoomAmount,
            bool keepBoundsInFrame, double durationScale, double positionDampingSeconds,
            double aimDampingSeconds, double accelerationLagSeconds, double maximumLagScale,
            double shakePositionScale, double shakeAngleDegrees, double shakeFrequency)
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
            KeepBoundsInFrame = keepBoundsInFrame;
            DurationScale = durationScale;
            PositionDampingSeconds = positionDampingSeconds;
            AimDampingSeconds = aimDampingSeconds;
            AccelerationLagSeconds = accelerationLagSeconds;
            MaximumLagScale = maximumLagScale;
            ShakePositionScale = shakePositionScale;
            ShakeAngleDegrees = shakeAngleDegrees;
            ShakeFrequency = shakeFrequency;
        }

        public static ShotProfile Get(ShotType type)
        {
            switch (type)
            {
                case ShotType.SideHighLeft:
                    return new ShotProfile(type, "Side High Left", -0.25, -0.90, 1.25, -0.35, false,
                        0.18, 0.08, 0.10, 0.0, -0.12, true, 0.22,
                        true, 0.95, 0.75, 0.55, 0.16, 0.22, 0.003, 0.10, 0.65);
                case ShotType.SideHighRight:
                    return new ShotProfile(type, "Side High Right", -0.25, 0.90, 1.25, 0.35, false,
                        0.18, 0.08, 0.10, 0.0, -0.12, true, 0.22,
                        true, 0.95, 0.75, 0.55, 0.16, 0.22, 0.003, 0.10, 0.65);
                case ShotType.FrontHighThreeQuarter:
                    return new ShotProfile(type, "Front High Three-Quarter", 1.0, 0.0, 1.1, -0.65, false,
                        0.22, 0.10, 0.25, 0.18, -0.15, false, 0.0,
                        true, 1.05, 0.85, 0.60, 0.18, 0.24, 0.003, 0.09, 0.55);
                case ShotType.OverheadDrift:
                    return new ShotProfile(type, "Overhead Drift", -0.15, 0.0, 2.4, 0.0, false,
                        0.08, 0.0, 0.05, 0.0, -0.06, false, 0.0,
                        true, 1.25, 1.30, 0.90, 0.14, 0.18, 0.002, 0.06, 0.35);
                case ShotType.WorldLockedFlyBy:
                    return new ShotProfile(type, "World-Locked Fly-By", 1.30, 0.45, 1.35, 0.0, true,
                        0.12, 0.08, 0.10, 0.0, -0.10, true, 0.25,
                        false, 0.80, 0.05, 0.35, 0.0, 0.0, 0.0, 0.0, 0.0);
                case ShotType.FrontMountedClose:
                    return new ShotProfile(type, "Front-Mounted Close", 0.0, 0.0, 0.0, 0.0, false,
                        0.02, 0.10, 0.15, -0.10, -0.08, false, 0.0,
                        false, 0.75, 0.05, 0.08, 0.0, 0.0, 0.008, 0.22, 2.8);
                case ShotType.RearMountedForward:
                    return new ShotProfile(type, "Rear-Mounted Forward", 0.0, 0.0, 0.0, 0.0, false,
                        0.28, 0.08, 0.0, 0.08, -0.10, false, 0.0,
                        false, 0.75, 0.05, 0.08, 0.0, 0.0, 0.007, 0.20, 2.5);
                case ShotType.DistantTopSide:
                    return new ShotProfile(type, "Distant Top-Side Establishing", -3.2, 2.2, 2.8, 0.0, false,
                        0.08, 0.03, 0.0, 0.18, -0.10, true, 0.18,
                        true, 1.45, 2.8, 1.8, 0.28, 0.38, 0.0, 0.0, 0.0);
                default:
                    return new ShotProfile(ShotType.RearHighThreeQuarter, "Rear High Three-Quarter",
                        -1.0, 0.0, 1.0, 0.65, false,
                        0.25, 0.08, 0.15, -0.18, -0.16, false, 0.0,
                        true, 1.15, 0.85, 0.60, 0.20, 0.26, 0.003, 0.09, 0.55);
            }
        }
    }
}
