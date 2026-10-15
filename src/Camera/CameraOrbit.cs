using System;
using SECinematicCamera.Director;
using VRageMath;

namespace SECinematicCamera.Camera
{
    public sealed class CameraOrbit
    {
        private double _yaw;
        private double _pitch;
        private double _targetYaw;
        private double _targetPitch;
        private double _yawVelocity;
        private double _pitchVelocity;
        private bool _returning;

        public bool IsEngaged { get; private set; }
        public bool HasOffset
        {
            get { return Math.Abs(_yaw) > 0.000001 || Math.Abs(_pitch) > 0.000001; }
        }

        public static bool SupportsShot(ShotType shot)
        {
            return shot == ShotType.Chase || shot == ShotType.RearHighThreeQuarter ||
                shot == ShotType.FrontHighThreeQuarter || shot == ShotType.SideHighLeft ||
                shot == ShotType.SideHighRight || shot == ShotType.DistantTopSide ||
                IsMountedShot(shot);
        }

        private static bool IsMountedShot(ShotType shot)
        {
            return shot == ShotType.FrontMountedClose || shot == ShotType.RearMountedForward;
        }

        public void Step(ShotType shot, bool held, Vector2 mouseDelta,
            double elapsedSeconds, double sensitivity, bool returnEnabled = true)
        {
            if (!SupportsShot(shot))
            {
                Clear();
                return;
            }
            if (elapsedSeconds <= 0.0)
                return;

            if (held)
            {
                // Mouse values are displacement, not velocity: do not multiply
                // by dt. Limit queued motion so a fast swipe cannot leave the
                // camera spinning long after the mouse has stopped.
                _targetYaw = _yaw + MathHelper.Clamp(
                    _targetYaw - _yaw + mouseDelta.X * sensitivity, -45.0, 45.0);
                _targetPitch = MathHelper.Clamp(_targetPitch - mouseDelta.Y * sensitivity,
                    IsMountedShot(shot) ? -65.0 : -35.0, IsMountedShot(shot) ? 65.0 : 55.0);
            }
            else if (returnEnabled)
            {
                // Return by the shortest yaw arc, including after a full orbit.
                _yaw = WrapDegrees(_yaw);
                _targetYaw = 0.0;
                _targetPitch = 0.0;
            }
            else if (_returning)
            {
                // Turning return OFF mid-recenter keeps the current angle.
                // Cancel the return target without changing the rendered pose.
                _targetYaw = _yaw;
                _targetPitch = _pitch;
                _yawVelocity = _pitchVelocity = 0.0;
            }
            _returning = !held && returnEnabled;

            double response = _returning ? 0.85 : 0.28;
            StepAngle(ref _yaw, ref _yawVelocity, _targetYaw, response, elapsedSeconds);
            StepAngle(ref _pitch, ref _pitchVelocity, _targetPitch, response, elapsedSeconds);
            if (held)
            {
                double turns = Math.Floor((_yaw + 180.0) / 360.0) * 360.0;
                _yaw -= turns;
                _targetYaw -= turns;
            }

            IsEngaged = held || Math.Abs(_targetYaw - _yaw) > 0.02 ||
                Math.Abs(_targetPitch - _pitch) > 0.02 ||
                Math.Abs(_yawVelocity) > 0.05 || Math.Abs(_pitchVelocity) > 0.05;
            if (!IsEngaged)
            {
                if (returnEnabled)
                    Clear();
                else
                {
                    // A retained angle must not pause the automatic director
                    // forever. Keep it for this shot after motion settles.
                    _yaw = _targetYaw;
                    _pitch = _targetPitch;
                    _yawVelocity = _pitchVelocity = 0.0;
                }
            }
        }

        public CameraPose Apply(CameraPose pose, Vector3D pivot, ShotType shot)
        {
            if (!SupportsShot(shot) || !HasOffset)
                return pose;

            if (IsMountedShot(shot))
                return ApplyMountedAim(pose);

            Vector3D up = Vector3D.Normalize(pose.Up);
            Vector3D offset = pose.Position - pivot;
            double radius = offset.Length();
            if (radius < 0.001)
                return pose;

            double vertical = Vector3D.Dot(offset, up);
            Vector3D horizontal = offset - up * vertical;
            if (horizontal.LengthSquared() < 0.001)
                return pose;
            horizontal.Normalize();

            double yaw = _yaw * Math.PI / 180.0;
            horizontal = horizontal * Math.Cos(yaw) +
                Vector3D.Cross(up, horizontal) * Math.Sin(yaw);
            double elevation = Math.Asin(MathHelper.Clamp(vertical / radius, -1.0, 1.0));
            // Bound elevation relative to this shot's Up, away from the pole.
            // This is an orbit limit, not a terrain or obstruction query.
            elevation = MathHelper.Clamp(elevation + _pitch * Math.PI / 180.0,
                Math.Min(elevation, 5.0 * Math.PI / 180.0),
                Math.Max(elevation, 75.0 * Math.PI / 180.0));
            pose.Position = pivot + (horizontal * Math.Cos(elevation) +
                up * Math.Sin(elevation)) * radius;
            return pose;
        }

        private CameraPose ApplyMountedAim(CameraPose pose)
        {
            Vector3D forward = pose.LookAt - pose.Position;
            double focusDistance = forward.Length();
            if (focusDistance < 0.001)
                return pose;
            forward /= focusDistance;
            Vector3D right = Vector3D.Cross(forward, pose.Up);
            if (right.LengthSquared() < 0.001)
                return pose;
            right.Normalize();
            Vector3D up = Vector3D.Normalize(Vector3D.Cross(right, forward));

            double yaw = _yaw * Math.PI / 180.0;
            double pitch = _pitch * Math.PI / 180.0;
            Vector3D yawForward = forward * Math.Cos(yaw) + right * Math.Sin(yaw);
            Vector3D aimedForward = yawForward * Math.Cos(pitch) + up * Math.Sin(pitch);
            // Pan/tilt in the mount's own frame. Position stays attached to the
            // same hull anchor; only direction and its orthogonal Up change.
            pose.LookAt = pose.Position + aimedForward * focusDistance;
            pose.Up = up * Math.Cos(pitch) - yawForward * Math.Sin(pitch);
            return pose;
        }

        private static void StepAngle(ref double angle, ref double velocity,
            double target, double responseSeconds, double elapsedSeconds)
        {
            double omega = 2.0 / responseSeconds;
            double acceleration = (target - angle) * omega * omega - 2.0 * omega * velocity;
            acceleration = MathHelper.Clamp(acceleration, -180.0, 180.0);
            velocity = MathHelper.Clamp(velocity + acceleration * elapsedSeconds, -60.0, 60.0);
            double previousError = target - angle;
            angle += velocity * elapsedSeconds;
            if (previousError * (target - angle) < 0.0)
            {
                angle = target;
                velocity = 0.0;
            }
        }

        private static double WrapDegrees(double angle)
        {
            return angle - Math.Floor((angle + 180.0) / 360.0) * 360.0;
        }

        public void Clear()
        {
            _yaw = _pitch = _targetYaw = _targetPitch = 0.0;
            _yawVelocity = _pitchVelocity = 0.0;
            _returning = false;
            IsEngaged = false;
        }
    }
}
