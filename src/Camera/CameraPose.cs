using VRageMath;

namespace SECinematicCamera.Camera
{
    public struct CameraPose
    {
        public Vector3D Position;
        public Vector3D LookAt;
        public Vector3D Up;

        public CameraPose(Vector3D position, Vector3D lookAt, Vector3D up)
        {
            Position = position;
            LookAt = lookAt;
            Up = up;
        }
    }
}
