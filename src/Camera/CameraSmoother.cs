using VRageMath;

namespace SECinematicCamera.Camera
{
    public sealed class CameraSmoother
    {
        private bool _hasPose;
        private CameraPose _pose;
        private Vector3D _targetAnchor;

        public CameraPose Reset(CameraPose pose, Vector3D targetAnchor)
        {
            _pose = pose;
            _targetAnchor = targetAnchor;
            _hasPose = true;
            return _pose;
        }

        public CameraPose Step(CameraPose desired, Vector3D targetAnchor,
            double smoothing, bool worldLockedPosition)
        {
            if (!_hasPose)
                return Reset(desired, targetAnchor);

            double amount = MathHelper.Clamp(smoothing, 0.01, 1.0);
            if (worldLockedPosition)
            {
                _pose.Position = Vector3D.Lerp(_pose.Position, desired.Position, amount);
            }
            else
            {
                Vector3D currentOffset = _pose.Position - _targetAnchor;
                Vector3D desiredOffset = desired.Position - targetAnchor;
                _pose.Position = targetAnchor + Vector3D.Lerp(currentOffset, desiredOffset, amount);
            }

            Vector3D currentAimOffset = _pose.LookAt - _targetAnchor;
            Vector3D desiredAimOffset = desired.LookAt - targetAnchor;
            _pose.LookAt = targetAnchor + Vector3D.Lerp(currentAimOffset, desiredAimOffset, amount);
            _pose.Up = Vector3D.Normalize(Vector3D.Lerp(_pose.Up, desired.Up, amount));
            _targetAnchor = targetAnchor;
            return _pose;
        }

        public void Clear()
        {
            _hasPose = false;
            _pose = default(CameraPose);
            _targetAnchor = Vector3D.Zero;
        }
    }
}
