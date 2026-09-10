using SECinematicCamera.Director;
using SECinematicCamera.Target;
using VRageMath;

namespace SECinematicCamera.Camera
{
    public sealed class CameraShake
    {
        public CameraPose Apply(CameraPose pose, CinematicTarget target,
            ShotProfile profile, double shotAgeSeconds)
        {
            if (profile.ShakeAngleDegrees <= 0.0 && profile.ShakePositionScale <= 0.0)
                return pose;

            Vector3D forward = pose.LookAt - pose.Position;
            double focusDistance = forward.Length();
            if (focusDistance < 0.01)
                return pose;
            forward /= focusDistance;

            Vector3D right = Vector3D.Cross(forward, pose.Up);
            if (right.LengthSquared() < 0.001)
                return pose;
            right.Normalize();
            Vector3D up = Vector3D.Cross(right, forward);
            up.Normalize();

            double speedGain = MathHelper.Clamp(0.35 + target.Speed / 110.0, 0.35, 1.35);
            double time = shotAgeSeconds * profile.ShakeFrequency;
            double yaw = LayeredWave(time, 0.17, 1.00, 2.37);
            double pitch = LayeredWave(time + 3.1, 0.13, 1.31, 2.83);
            double roll = LayeredWave(time + 7.4, 0.11, 0.83, 3.17);
            double angle = MathHelper.ToRadians((float)(profile.ShakeAngleDegrees * speedGain));

            Vector3D shakenLookAt = pose.LookAt +
                right * yaw * angle * focusDistance +
                up * pitch * angle * focusDistance;
            Vector3D shakenUp = Vector3D.Normalize(up + right * roll * angle * 0.7);

            double positionAmplitude = target.Scale * profile.ShakePositionScale * speedGain;
            Vector3D shakenPosition = pose.Position +
                right * LayeredWave(time + 1.7, 0.23, 1.19, 2.11) * positionAmplitude +
                up * LayeredWave(time + 5.3, 0.19, 0.97, 2.71) * positionAmplitude * 0.7;
            return new CameraPose(shakenPosition, shakenLookAt, shakenUp);
        }

        private static double LayeredWave(double time, double low, double middle, double high)
        {
            return System.Math.Sin(time * low * 6.283185307) * 0.50 +
                   System.Math.Sin(time * middle * 6.283185307 + 1.73) * 0.32 +
                   System.Math.Sin(time * high * 6.283185307 + 4.21) * 0.18;
        }
    }
}
