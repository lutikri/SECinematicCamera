using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;

namespace SECinematicCamera.Target
{
    public sealed class FramingTarget
    {
        private readonly Vector3D[] _corners = new Vector3D[8];

        public Vector3D Center { get; private set; }
        public Vector3D Cockpit { get; private set; }
        public Vector3D Nose { get; private set; }
        public Vector3D Tail { get; private set; }
        public Vector3D Forward { get; private set; }
        public Vector3D Up { get; private set; }
        public Vector3D Right { get; private set; }
        public double HalfLength { get; private set; }
        public double HalfHeight { get; private set; }
        public double HalfWidth { get; private set; }
        public double Radius { get; private set; }
        public Vector3D[] Corners { get { return _corners; } }

        public void Refresh(IMyShipController controller, IMyCubeGrid grid)
        {
            BoundingBox localBounds = grid.LocalAABB;
            Vector3 localCenter = localBounds.Center;
            Vector3 localHalf = localBounds.HalfExtents;
            MatrixD world = grid.WorldMatrix;

            Right = Vector3D.Normalize(world.Right);
            Up = Vector3D.Normalize(world.Up);
            Forward = Vector3D.Normalize(world.Forward);
            Center = Vector3D.Transform((Vector3D)localCenter, world);
            Cockpit = controller.GetPosition();

            HalfWidth = System.Math.Max(0.5, localHalf.X);
            HalfHeight = System.Math.Max(0.5, localHalf.Y);
            HalfLength = System.Math.Max(0.5, localHalf.Z);
            Radius = System.Math.Max(1.0,
                System.Math.Sqrt(HalfWidth * HalfWidth + HalfHeight * HalfHeight + HalfLength * HalfLength));

            Nose = Center + Forward * HalfLength;
            Tail = Center - Forward * HalfLength;

            Vector3D backward = -Forward;
            int index = 0;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        _corners[index++] = Center +
                            Right * HalfWidth * x +
                            Up * HalfHeight * y +
                            backward * HalfLength * z;
                    }
                }
            }
        }

        public void Clear()
        {
            Center = Vector3D.Zero;
            Cockpit = Vector3D.Zero;
            Nose = Vector3D.Zero;
            Tail = Vector3D.Zero;
            Forward = Vector3D.Forward;
            Up = Vector3D.Up;
            Right = Vector3D.Right;
            HalfLength = 0;
            HalfHeight = 0;
            HalfWidth = 0;
            Radius = 0;
            for (int index = 0; index < _corners.Length; index++)
                _corners[index] = Vector3D.Zero;
        }
    }
}
