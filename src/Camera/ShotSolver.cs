using Sandbox.ModAPI;
using SECinematicCamera.Config;
using SECinematicCamera.Director;
using SECinematicCamera.Target;
using VRageMath;

namespace SECinematicCamera.Camera
{
    public sealed class ShotSolver
    {
        private readonly CinematicConfig _config;
        private ShotType _activeShot;
        private bool _hasActiveShot;
        private Vector3D _worldLockedPosition;

        public ShotSolver(CinematicConfig config)
        {
            _config = config;
        }

        public CameraPose Solve(CinematicTarget target, ShotType shot)
        {
            ShotProfile profile = ShotProfile.Get(shot);
            Vector3D up;
            Vector3D forward;
            Vector3D right;
            ResolveFrame(target, out up, out forward, out right);
            Vector3D lookAt = target.Position + (Vector3D)target.LinearVelocity * _config.LookAheadSeconds;
            double distance = target.Scale * _config.Distance;
            double height = target.Scale * _config.Height;
            double side = target.Scale * _config.Side;

            Vector3D position = target.Position +
                forward * distance * profile.ForwardDistance +
                right * (distance * profile.RightDistance + side * profile.SideBias) +
                up * height * profile.HeightScale;

            if (profile.WorldLocked && _hasActiveShot && _activeShot == shot)
                position = _worldLockedPosition;

            return new CameraPose(position, lookAt, up);
        }

        public void BeginShot(CinematicTarget target, ShotType shot)
        {
            _activeShot = shot;
            _hasActiveShot = true;
            if (ShotProfile.Get(shot).WorldLocked)
            {
                _hasActiveShot = false;
                _worldLockedPosition = Solve(target, shot).Position;
                _hasActiveShot = true;
            }
        }

        public void Clear()
        {
            _activeShot = ShotType.RearHighThreeQuarter;
            _hasActiveShot = false;
            _worldLockedPosition = Vector3D.Zero;
        }

        private static void ResolveFrame(CinematicTarget target, out Vector3D up,
            out Vector3D forward, out Vector3D right)
        {
            float interference;
            Vector3 gravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(target.Position, out interference);
            if (gravity.LengthSquared() > 0.0001f)
            {
                up = -Vector3D.Normalize(gravity);
                forward = target.WorldMatrix.Forward -
                    up * Vector3D.Dot(target.WorldMatrix.Forward, up);
                if (forward.LengthSquared() < 0.01)
                    forward = target.WorldMatrix.Up - up * Vector3D.Dot(target.WorldMatrix.Up, up);
                if (forward.LengthSquared() < 0.01)
                    forward = Vector3D.CalculatePerpendicularVector(up);
                forward.Normalize();
                right = Vector3D.Cross(forward, up);
                right.Normalize();
                return;
            }

            up = target.WorldMatrix.Up;
            forward = target.WorldMatrix.Forward;
            right = target.WorldMatrix.Right;
            if (up.LengthSquared() < 0.5 || forward.LengthSquared() < 0.5 || right.LengthSquared() < 0.5)
            {
                up = Vector3D.Up;
                forward = Vector3D.Forward;
                right = Vector3D.Right;
            }
            else
            {
                up.Normalize();
                forward.Normalize();
                right.Normalize();
            }
        }
    }
}
