using System;
using VRageMath;

namespace SECinematicCamera.Camera
{
    public static class CameraMotion
    {
        public static Vector3D RotateTowards(Vector3D from, Vector3D to,
            Vector3D preferredUp, double maximumRadians)
        {
            from.Normalize();
            to.Normalize();
            double dot = MathHelper.Clamp(Vector3D.Dot(from, to), -1.0, 1.0);
            double angle = Math.Acos(dot);
            if (angle <= maximumRadians || angle < 0.000001)
                return to;
            if (maximumRadians <= 0.0)
                return from;

            Vector3D axis = Vector3D.Cross(from, to);
            if (axis.LengthSquared() < 0.000001)
            {
                // Opposite directions have no unique rotation axis. Choose a
                // stable yaw around Up instead of normalizing a zero-vector blend.
                axis = preferredUp - from * Vector3D.Dot(preferredUp, from);
                if (axis.LengthSquared() < 0.000001)
                    axis = Vector3D.CalculatePerpendicularVector(from);
            }
            axis.Normalize();
            Vector3D rotated = from * Math.Cos(maximumRadians) +
                Vector3D.Cross(axis, from) * Math.Sin(maximumRadians) +
                axis * Vector3D.Dot(axis, from) * (1.0 - Math.Cos(maximumRadians));
            return Vector3D.Normalize(rotated);
        }
    }
}
