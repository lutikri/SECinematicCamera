using VRageMath;
using SECinematicCamera.Director;

namespace SECinematicCamera.Camera
{
    public sealed class CameraSmoother
    {
        private bool _hasPose;
        private CameraPose _pose;
        private Vector3D _targetAnchor;
        private Vector3D _previousVelocity;
        private Vector3D _filteredAcceleration;
        private Vector3D _accelerationOffset;
        private bool _hasVelocity;

        public CameraPose Reset(CameraPose pose, Vector3D targetAnchor)
        {
            _pose = pose;
            _targetAnchor = targetAnchor;
            _hasPose = true;
            _hasVelocity = false;
            _filteredAcceleration = Vector3D.Zero;
            _accelerationOffset = Vector3D.Zero;
            return _pose;
        }

        public CameraPose Step(CameraPose desired, Vector3D targetAnchor,
            Vector3D targetVelocity, double elapsedSeconds, double smoothing,
            ShotProfile profile, double targetScale)
        {
            if (!_hasPose)
                return Reset(desired, targetAnchor);

            double responseScale = MathHelper.Clamp(smoothing / 0.16, 0.10, 6.0);
            double positionAmount = DampAmount(profile.PositionDampingSeconds,
                elapsedSeconds, responseScale);
            double aimAmount = DampAmount(profile.AimDampingSeconds,
                elapsedSeconds, responseScale);

            Vector3D previousAccelerationOffset = _accelerationOffset;
            UpdateAccelerationOffset(targetVelocity, elapsedSeconds, profile, targetScale);

            if (profile.WorldLocked)
            {
                _pose.Position = desired.Position;
            }
            else
            {
                Vector3D currentOffset = _pose.Position - _targetAnchor - previousAccelerationOffset;
                Vector3D desiredOffset = desired.Position - targetAnchor;
                _pose.Position = targetAnchor +
                    Vector3D.Lerp(currentOffset, desiredOffset, positionAmount) +
                    _accelerationOffset;
            }

            Vector3D currentAimOffset = _pose.LookAt - _targetAnchor;
            Vector3D desiredAimOffset = desired.LookAt - targetAnchor;
            _pose.LookAt = targetAnchor + Vector3D.Lerp(currentAimOffset, desiredAimOffset, aimAmount);
            _pose.Up = Vector3D.Normalize(Vector3D.Lerp(_pose.Up, desired.Up, aimAmount));
            _targetAnchor = targetAnchor;
            return _pose;
        }

        private void UpdateAccelerationOffset(Vector3D velocity, double elapsedSeconds,
            ShotProfile profile, double targetScale)
        {
            if (!_hasVelocity || elapsedSeconds <= 0.0001)
            {
                _previousVelocity = velocity;
                _hasVelocity = true;
                _filteredAcceleration = Vector3D.Zero;
                _accelerationOffset = Vector3D.Zero;
                return;
            }

            Vector3D acceleration = (velocity - _previousVelocity) / elapsedSeconds;
            _previousVelocity = velocity;
            double maximumAcceleration = 120.0;
            if (acceleration.LengthSquared() > maximumAcceleration * maximumAcceleration)
                acceleration = Vector3D.Normalize(acceleration) * maximumAcceleration;

            double filterAmount = DampAmount(0.18, elapsedSeconds, 1.0);
            _filteredAcceleration = Vector3D.Lerp(_filteredAcceleration, acceleration, filterAmount);
            Vector3D desiredLag = -_filteredAcceleration *
                profile.AccelerationLagSeconds * profile.AccelerationLagSeconds;
            double maximumLag = targetScale * profile.MaximumLagScale;
            if (maximumLag <= 0.0)
                desiredLag = Vector3D.Zero;
            else if (desiredLag.LengthSquared() > maximumLag * maximumLag)
                desiredLag = Vector3D.Normalize(desiredLag) * maximumLag;

            double lagAmount = DampAmount(0.28, elapsedSeconds, 1.0);
            _accelerationOffset = Vector3D.Lerp(_accelerationOffset, desiredLag, lagAmount);
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
            _targetAnchor = Vector3D.Zero;
            _previousVelocity = Vector3D.Zero;
            _filteredAcceleration = Vector3D.Zero;
            _accelerationOffset = Vector3D.Zero;
            _hasVelocity = false;
        }
    }
}
