using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;

namespace SECinematicCamera.Target
{
    public sealed class CinematicTarget
    {
        public IMyShipController Controller { get; private set; }
        public IMyCubeGrid Grid { get; private set; }
        public long ControlledEntityId { get; private set; }
        public long GridEntityId { get; private set; }
        public Vector3D Position { get; private set; }
        public MatrixD WorldMatrix { get; private set; }
        public Vector3 LinearVelocity { get; private set; }
        public Vector3 AngularVelocity { get; private set; }
        public double Scale { get; private set; }

        public bool IsValid
        {
            get
            {
                return Controller != null && Grid != null && !Controller.Closed && !Controller.MarkedForClose &&
                       !Grid.Closed && !Grid.MarkedForClose;
            }
        }

        public double Speed { get { return LinearVelocity.Length(); } }
        public double AngularSpeed { get { return AngularVelocity.Length(); } }

        public CinematicTarget(IMyShipController controller, IMyCubeGrid grid)
        {
            Controller = controller;
            Grid = grid;
            ControlledEntityId = controller.EntityId;
            GridEntityId = grid.EntityId;
            Refresh();
        }

        public void Refresh()
        {
            if (!IsValid)
                return;

            Position = Grid.WorldAABB.Center;
            WorldMatrix = Grid.WorldMatrix;
            LinearVelocity = Grid.Physics != null ? Grid.Physics.LinearVelocity : Vector3.Zero;
            AngularVelocity = Grid.Physics != null ? Grid.Physics.AngularVelocity : Vector3.Zero;
            Scale = System.Math.Max(5.0, Grid.WorldAABB.HalfExtents.Length());
        }

        public void Clear()
        {
            Controller = null;
            Grid = null;
            ControlledEntityId = 0;
            GridEntityId = 0;
            Position = Vector3D.Zero;
            WorldMatrix = MatrixD.Identity;
            LinearVelocity = Vector3.Zero;
            AngularVelocity = Vector3.Zero;
            Scale = 0;
        }
    }
}
