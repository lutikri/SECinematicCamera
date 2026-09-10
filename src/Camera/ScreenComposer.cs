using SECinematicCamera.Director;
using SECinematicCamera.Target;
using System.Globalization;
using VRageMath;

namespace SECinematicCamera.Camera
{
    public sealed class ScreenComposer
    {
        private const double HardScreenLimit = 0.90;

        public double LastScreenX { get; private set; }
        public double LastScreenY { get; private set; }
        public double LastBoundsCorrection { get; private set; }

        public CameraPose Compose(CameraPose basePose, CinematicTarget target,
            ShotProfile profile, CameraProjectionInfo projection)
        {
            FramingTarget framing = target.Framing;
            Vector3D focus = ResolveFocus(basePose.LookAt, framing, profile);
            double screenX = ResolveScreenX(basePose.Position, focus, target, profile);
            LastScreenX = screenX;
            LastScreenY = profile.ScreenY;
            LastBoundsCorrection = 1.0;
            CameraPose composed = AimForScreenPoint(basePose.Position, focus, basePose.Up,
                screenX, profile.ScreenY, projection);

            if (!profile.WorldLocked)
            {
                double overflow = MeasureBoundsOverflow(composed, framing.Corners, projection);
                if (overflow > 1.0)
                {
                    double correction = System.Math.Min(2.5, overflow * 1.04);
                    LastBoundsCorrection = correction;
                    Vector3D away = composed.Position - focus;
                    if (away.LengthSquared() > 0.001)
                    {
                        Vector3D correctedPosition = focus + away * correction;
                        composed = AimForScreenPoint(correctedPosition, focus, basePose.Up,
                            screenX, profile.ScreenY, projection);
                    }
                }
            }

            return composed;
        }

        public string GetDiagnostics()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "frame ({0:0.00}, {1:0.00}); bounds correction {2:0.00}x",
                LastScreenX, LastScreenY, LastBoundsCorrection);
        }

        private static Vector3D ResolveFocus(Vector3D motionFocus, FramingTarget framing,
            ShotProfile profile)
        {
            Vector3D focus = motionFocus +
                framing.Forward * framing.HalfLength * profile.FocusForward +
                framing.Up * framing.HalfHeight * profile.FocusUp;

            if (profile.CockpitWeight > 0.0)
                focus = Vector3D.Lerp(focus, framing.Cockpit, profile.CockpitWeight);
            return focus;
        }

        private static double ResolveScreenX(Vector3D cameraPosition, Vector3D focus,
            CinematicTarget target, ShotProfile profile)
        {
            if (!profile.UseLeadRoom)
                return profile.ScreenX;

            Vector3D viewForward = focus - cameraPosition;
            if (viewForward.LengthSquared() < 0.001)
                return profile.ScreenX;
            viewForward.Normalize();

            Vector3D viewRight = Vector3D.Cross(viewForward, target.Framing.Up);
            if (viewRight.LengthSquared() < 0.001)
                return profile.ScreenX;
            viewRight.Normalize();

            Vector3D actionDirection = target.Speed > 1.0
                ? Vector3D.Normalize((Vector3D)target.LinearVelocity)
                : target.Framing.Forward;
            double acrossScreen = Vector3D.Dot(actionDirection, viewRight);
            if (System.Math.Abs(acrossScreen) < 0.10)
                return profile.ScreenX;

            // Put the subject opposite its screen-space travel direction, leaving
            // visible room in front of it.
            return acrossScreen > 0.0 ? -profile.LeadRoomAmount : profile.LeadRoomAmount;
        }

        private static CameraPose AimForScreenPoint(Vector3D cameraPosition, Vector3D focus,
            Vector3D preferredUp, double screenX, double screenY, CameraProjectionInfo projection)
        {
            Vector3D centeredForward = focus - cameraPosition;
            double depth = centeredForward.Length();
            if (depth < 0.001)
                return new CameraPose(cameraPosition, focus, preferredUp);
            centeredForward /= depth;

            Vector3D right = Vector3D.Cross(centeredForward, preferredUp);
            if (right.LengthSquared() < 0.001)
                right = Vector3D.CalculatePerpendicularVector(centeredForward);
            right.Normalize();
            Vector3D cameraUp = Vector3D.Cross(right, centeredForward);
            cameraUp.Normalize();

            Vector3D virtualTarget = focus -
                right * screenX * depth * projection.TanHalfHorizontalFov -
                cameraUp * screenY * depth * projection.TanHalfVerticalFov;
            return new CameraPose(cameraPosition, virtualTarget, preferredUp);
        }

        private static double MeasureBoundsOverflow(CameraPose pose, Vector3D[] corners,
            CameraProjectionInfo projection)
        {
            Vector3D forward = pose.LookAt - pose.Position;
            if (forward.LengthSquared() < 0.001)
                return 1.0;
            forward.Normalize();

            Vector3D right = Vector3D.Cross(forward, pose.Up);
            if (right.LengthSquared() < 0.001)
                return 1.0;
            right.Normalize();
            Vector3D up = Vector3D.Cross(right, forward);
            up.Normalize();

            double maximum = 0.0;
            for (int index = 0; index < corners.Length; index++)
            {
                Vector3D relative = corners[index] - pose.Position;
                double depth = Vector3D.Dot(relative, forward);
                if (depth <= 0.05)
                    return 2.5;

                double x = System.Math.Abs(Vector3D.Dot(relative, right) /
                    (depth * projection.TanHalfHorizontalFov));
                double y = System.Math.Abs(Vector3D.Dot(relative, up) /
                    (depth * projection.TanHalfVerticalFov));
                maximum = System.Math.Max(maximum, System.Math.Max(x, y));
            }

            return maximum / HardScreenLimit;
        }
    }
}
