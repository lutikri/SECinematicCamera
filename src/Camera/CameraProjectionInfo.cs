namespace SECinematicCamera.Camera
{
    public struct CameraProjectionInfo
    {
        public double TanHalfHorizontalFov;
        public double TanHalfVerticalFov;

        public CameraProjectionInfo(double tanHalfHorizontalFov, double tanHalfVerticalFov)
        {
            TanHalfHorizontalFov = tanHalfHorizontalFov;
            TanHalfVerticalFov = tanHalfVerticalFov;
        }
    }
}
