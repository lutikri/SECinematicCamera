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

        public ShotSolver(CinematicConfig config)
        {
            _config = config;
        }

        public CameraPose Solve(CinematicTarget target, ShotType shot)
        {
            Vector3D up = ResolveUp(target);
            Vector3D backward = target.WorldMatrix.Backward;
            Vector3D right = target.WorldMatrix.Right;
            Vector3D lookAt = target.Position + (Vector3D)target.LinearVelocity * _config.LookAheadSeconds;
            double distance = target.Scale * _config.Distance;
            double height = target.Scale * _config.Height;
            double side = target.Scale * _config.Side;

            // First milestone intentionally supports only the safest profile.
            Vector3D position = target.Position + backward * distance + up * height + right * side;
            return new CameraPose(position, lookAt, up);
        }

        private static Vector3D ResolveUp(CinematicTarget target)
        {
            float interference;
            Vector3 gravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(target.Position, out interference);
            if (gravity.LengthSquared() > 0.0001f)
                return -Vector3D.Normalize(gravity);

            Vector3D up = target.WorldMatrix.Up;
            return up.LengthSquared() > 0.5 ? Vector3D.Normalize(up) : Vector3D.Up;
        }
    }
}
