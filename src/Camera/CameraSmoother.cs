using VRageMath;
using SECinematicCamera.Director;
using System.Globalization;

namespace SECinematicCamera.Camera
{
    public sealed class CameraSmoother
    {
        private bool _hasPose;
        private CameraPose _pose;
        private Vector3D _followAnchor;
        private Vector3D _followVelocity;
        private Vector3D _relativePosition;
        private bool _hasFollowVelocity;
        private Vector3D _aimAnchor;
        private Vector3D _relativeAim;
        private Vector3D _observedTargetVelocity;
        private bool _hasObservedVelocity;

        public double LastPositionLag { get; private set; }
        public double LastAimLag { get; private set; }

        public CameraPose Reset(CameraPose pose, Vector3D targetAnchor)
        {
            _pose = pose;
            _followAnchor = targetAnchor;
            _relativePosition = pose.Position - targetAnchor;
            _aimAnchor = targetAnchor;
            _relativeAim = pose.LookAt - targetAnchor;
            _hasPose = true;
            _hasFollowVelocity = false;
            _followVelocity = Vector3D.Zero;
            _observedTargetVelocity = Vector3D.Zero;
            _hasObservedVelocity = false;
            LastPositionLag = 0.0;
            LastAimLag = 0.0;
            return _pose;
        }

        public CameraPose Step(CameraPose desired, Vector3D targetAnchor,
            Vector3D targetVelocity, double elapsedSeconds, double smoothing,
            ShotProfile profile, double targetScale, bool orbitEngaged = false)
        {
            if (!_hasPose)
                return Reset(desired, targetAnchor);

            double responseScale = MathHelper.Clamp(smoothing / 0.16, 0.10, 6.0);
            // Mouse orbit has its own angular inertia. Keep a small pose filter
            // while adjusting/returning instead of stacking the establishing
            // shot's several-second rotation lag on top of the user's drag.
            double positionDamping = orbitEngaged
                ? System.Math.Min(profile.PositionDampingSeconds, 0.20) : profile.PositionDampingSeconds;
            double aimDamping = orbitEngaged
                ? System.Math.Min(profile.AimDampingSeconds, 0.20) : profile.AimDampingSeconds;
            double positionAmount = DampAmount(positionDamping,
                elapsedSeconds, responseScale);
            double aimAmount = DampAmount(aimDamping,
                elapsedSeconds, responseScale);
            if (!_hasObservedVelocity)
            {
                _observedTargetVelocity = targetVelocity;
                _hasObservedVelocity = true;
            }
            else
            {
                // Deliberately learn target velocity slowly. Feeding raw velocity
                // into the pursuit spring makes every thruster key transition
                // feel like a camera command even though it is physically derived.
                double velocityAmount = DampAmount(1.10, elapsedSeconds, 1.0);
                _observedTargetVelocity = Vector3D.Lerp(_observedTargetVelocity,
                    targetVelocity, velocityAmount);
            }

            if (profile.WorldLocked)
            {
                _pose.Position = desired.Position;
            }
            else
            {
                Vector3D desiredOffset = desired.Position - targetAnchor;
                _relativePosition = Vector3D.Lerp(_relativePosition, desiredOffset, positionAmount);
                UpdateSpring(ref _followAnchor, ref _followVelocity, ref _hasFollowVelocity,
                    targetAnchor, _observedTargetVelocity, elapsedSeconds,
                    profile.TranslationFollowSeconds, targetScale * profile.MaximumLagScale);
                _pose.Position = _followAnchor + _relativePosition;
            }

            Vector3D desiredAimOffset = desired.LookAt - targetAnchor;
            _relativeAim = Vector3D.Lerp(_relativeAim, desiredAimOffset, aimAmount);
            if (profile.WorldLocked)
            {
                _aimAnchor = targetAnchor;
                _pose.LookAt = SmoothWorldLockedAim(_pose, desired, aimAmount);
            }
            else
            {
                // Position and aim ride the same operator rig. Two pursuit
                // springs with different response times turn each acceleration
                // into an unintended pan, even when the desired angle is fixed.
                _aimAnchor = _followAnchor;
                _pose.LookAt = _aimAnchor + _relativeAim;
            }
            _pose.Up = Vector3D.Normalize(Vector3D.Lerp(_pose.Up, desired.Up, aimAmount));
            LastPositionLag = profile.WorldLocked
                ? 0.0
                : Vector3D.Distance(_followAnchor, targetAnchor);
            LastAimLag = profile.WorldLocked
                ? 0.0
                : Vector3D.Distance(_aimAnchor, targetAnchor);
            return _pose;
        }

        private static Vector3D SmoothWorldLockedAim(CameraPose current, CameraPose desired,
            double amount)
        {
            Vector3D currentDirection = current.LookAt - current.Position;
            Vector3D desiredDirection = desired.LookAt - desired.Position;
            double desiredDistance = desiredDirection.Length();
            if (currentDirection.LengthSquared() < 0.001 || desiredDistance < 0.001)
                return desired.LookAt;

            currentDirection.Normalize();
            desiredDirection /= desiredDistance;
            Vector3D direction = Vector3D.Lerp(currentDirection, desiredDirection, amount);
            if (direction.LengthSquared() < 0.001)
                direction = desiredDirection;
            else
                direction.Normalize();
            return current.Position + direction * desiredDistance;
        }

        public string GetDiagnostics()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "operator lag pos {0:0.00} m, aim {1:0.00} m",
                LastPositionLag, LastAimLag);
        }

        private static void UpdateSpring(ref Vector3D position, ref Vector3D velocity,
            ref bool initialized, Vector3D targetPosition, Vector3D targetVelocity,
            double elapsedSeconds, double followSeconds, double maximumLag)
        {
            if (!initialized || elapsedSeconds <= 0.0001)
            {
                position = targetPosition;
                velocity = targetVelocity;
                initialized = true;
                return;
            }

            if (followSeconds <= 0.001 || maximumLag <= 0.0)
            {
                position = targetPosition;
                velocity = targetVelocity;
                return;
            }

            // Critically damped world-space pursuit. The camera has its own
            // velocity; it responds to the ship's observed motion, never input.
            double omega = 2.0 / followSeconds;
            Vector3D error = targetPosition - position;
            double normalizedLag = error.Length() / maximumLag;
            double edge = MathHelper.Clamp((normalizedLag - 0.55) / 0.45, 0.0, 1.0);
            omega *= 1.0 + edge * edge * 2.0;
            Vector3D acceleration = error * omega * omega +
                (targetVelocity - velocity) * (2.0 * omega);
            velocity += acceleration * elapsedSeconds;
            position += velocity * elapsedSeconds;

            error = targetPosition - position;
            if (error.LengthSquared() > maximumLag * maximumLag)
            {
                Vector3D direction = Vector3D.Normalize(error);
                position = targetPosition - direction * maximumLag;

                // At the framing limit, discard only the velocity component
                // that would increase separation. Tangential camera motion stays.
                Vector3D relativeVelocity = velocity - targetVelocity;
                double separatingSpeed = Vector3D.Dot(relativeVelocity, direction);
                if (separatingSpeed < 0.0)
                    relativeVelocity -= direction * separatingSpeed;
                velocity = targetVelocity + relativeVelocity;
            }
        }

        private static double DampAmount(double dampingSeconds, double elapsedSeconds,
            double responseScale)
        {
            if (dampingSeconds <= 0.001)
                return 1.0;
            return 1.0 - System.Math.Exp(-elapsedSeconds * responseScale / dampingSeconds);
        }

        public void Clear()
        {
            _hasPose = false;
            _pose = default(CameraPose);
            _followAnchor = Vector3D.Zero;
            _followVelocity = Vector3D.Zero;
            _relativePosition = Vector3D.Zero;
            _hasFollowVelocity = false;
            _aimAnchor = Vector3D.Zero;
            _relativeAim = Vector3D.Zero;
            _observedTargetVelocity = Vector3D.Zero;
            _hasObservedVelocity = false;
            LastPositionLag = 0.0;
            LastAimLag = 0.0;
        }
    }
}
