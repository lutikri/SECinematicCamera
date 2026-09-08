using VRageMath;

namespace SECinematicCamera.Camera
{
    public sealed class CameraSmoother
    {
        private bool _hasPose;
        private CameraPose _pose;

        public CameraPose Reset(CameraPose pose)
        {
            _pose = pose;
            _hasPose = true;
            return _pose;
        }

        public CameraPose Step(CameraPose desired, double smoothing)
        {
            if (!_hasPose)
                return Reset(desired);

            double amount = MathHelper.Clamp(smoothing, 0.01, 1.0);
            _pose.Position = Vector3D.Lerp(_pose.Position, desired.Position, amount);
            _pose.LookAt = Vector3D.Lerp(_pose.LookAt, desired.LookAt, amount);
            _pose.Up = Vector3D.Normalize(Vector3D.Lerp(_pose.Up, desired.Up, amount));
            return _pose;
        }

        public void Clear()
        {
            _hasPose = false;
            _pose = default(CameraPose);
        }
    }
}
