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
        private double _worldLockedClosestDistance;

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
            Vector3D lookAhead = (Vector3D)target.LinearVelocity * _config.LookAheadSeconds;
            double maximumLookAhead = System.Math.Max(0.5, target.Scale * 0.15);
            if (lookAhead.LengthSquared() > maximumLookAhead * maximumLookAhead)
                lookAhead = Vector3D.Normalize(lookAhead) * maximumLookAhead;

            Vector3D lookAt = target.Position + lookAhead;

            if (shot == ShotType.FrontMountedClose || shot == ShotType.RearMountedForward)
                return SolveMounted(target, shot);

            if (shot == ShotType.WorldLockedFlyBy)
            {
                Vector3D flyByPosition = _hasActiveShot && _activeShot == shot
                    ? _worldLockedPosition
                    : SolveFlyByPosition(target, up, forward, right);
                return new CameraPose(flyByPosition, lookAt, up);
            }

            double distance = target.Scale * _config.Distance;
            // Distance is the overall shot scale. Height and side are ratios of it,
            // so lowering distance brings the whole rig closer rather than changing
            // only its fore/aft component.
            double height = distance * _config.Height;
            double side = distance * _config.Side;

            Vector3D position = target.Position +
                forward * distance * profile.ForwardDistance +
                right * (distance * profile.RightDistance + side * profile.SideBias) +
                up * height * profile.HeightScale;

            if (profile.WorldLocked && _hasActiveShot && _activeShot == shot)
                position = _worldLockedPosition;

            return new CameraPose(position, lookAt, up);
        }

        private static Vector3D SolveFlyByPosition(CinematicTarget target, Vector3D up,
            Vector3D forward, Vector3D right)
        {
            double placementDistance = System.Math.Max(60.0,
                System.Math.Min(100.0, target.Scale * 2.25));
            Vector3D direction = forward * 0.72 + right * 0.48 + up * 0.50;
            direction.Normalize();
            return target.Position + direction * placementDistance;
        }

        private static CameraPose SolveMounted(CinematicTarget target, ShotType shot)
        {
            FramingTarget framing = target.Framing;
            Vector3D viewForward = target.Controller.WorldMatrix.Forward;
            Vector3D viewUp = target.Controller.WorldMatrix.Up;
            Vector3D viewRight = target.Controller.WorldMatrix.Right;
            viewForward.Normalize();
            viewUp.Normalize();
            viewRight.Normalize();

            // Project the grid OBB onto the cockpit frame. This gives stable
            // outer surfaces for arbitrary cockpit mounting without scanning
            // individual blocks or adding collision queries.
            double forwardSupport = System.Math.Abs(Vector3D.Dot(viewForward, framing.Forward)) * framing.HalfLength +
                System.Math.Abs(Vector3D.Dot(viewForward, framing.Up)) * framing.HalfHeight +
                System.Math.Abs(Vector3D.Dot(viewForward, framing.Right)) * framing.HalfWidth;
            double rightSupport = System.Math.Abs(Vector3D.Dot(viewRight, framing.Forward)) * framing.HalfLength +
                System.Math.Abs(Vector3D.Dot(viewRight, framing.Up)) * framing.HalfHeight +
                System.Math.Abs(Vector3D.Dot(viewRight, framing.Right)) * framing.HalfWidth;
            double upSupport = System.Math.Abs(Vector3D.Dot(viewUp, framing.Forward)) * framing.HalfLength +
                System.Math.Abs(Vector3D.Dot(viewUp, framing.Up)) * framing.HalfHeight +
                System.Math.Abs(Vector3D.Dot(viewUp, framing.Right)) * framing.HalfWidth;
            double outwardOffset = System.Math.Max(target.Grid.GridSize * 1.25, target.Scale * 0.035);
            double verticalOffset = System.Math.Max(target.Grid.GridSize * 0.35, target.Scale * 0.008);
            double focusDistance = System.Math.Max(50.0, target.Scale * 4.0);
            Vector3D position;

            if (shot == ShotType.FrontMountedClose)
            {
                // Right-side body mount at the cockpit's longitudinal/vertical
                // station. The camera sits just outside the rightmost OBB surface
                // and looks straight forward along the pilot's view axis.
                Vector3D cockpitRelative = framing.Cockpit - framing.Center;
                double cockpitForward = MathHelper.Clamp(
                    Vector3D.Dot(cockpitRelative, viewForward),
                    -forwardSupport * 0.80, forwardSupport * 0.80);
                double cockpitUp = MathHelper.Clamp(
                    Vector3D.Dot(cockpitRelative, viewUp),
                    -upSupport * 0.75, upSupport * 0.75);
                position = framing.Center +
                    viewForward * cockpitForward +
                    viewUp * (cockpitUp + verticalOffset) +
                    viewRight * (rightSupport + outwardOffset);
            }
            else
            {
                // Rear-left corner mount: beyond both the tail and left hull
                // surfaces, lifted slightly so the nearby hull reads clearly.
                position = framing.Center -
                    viewForward * (forwardSupport + outwardOffset * 0.70) -
                    viewRight * (rightSupport + outwardOffset) +
                    viewUp * (upSupport * 0.18 + verticalOffset);
            }

            // Mounted cameras look parallel to the cockpit forward direction.
            // They never aim back at the cockpit or grid center.
            return new CameraPose(position, position + viewForward * focusDistance, viewUp);
        }

        public void BeginShot(CinematicTarget target, ShotType shot)
        {
            _activeShot = shot;
            _hasActiveShot = true;
            if (ShotProfile.Get(shot).WorldLocked)
            {
                _hasActiveShot = false;
                _worldLockedPosition = Solve(target, shot).Position;
                _worldLockedClosestDistance = Vector3D.Distance(_worldLockedPosition, target.Position);
                _hasActiveShot = true;
            }
        }

        public bool ShouldEndWorldLockedShot(CinematicTarget target, double exitDistance)
        {
            if (!_hasActiveShot || _activeShot != ShotType.WorldLockedFlyBy)
                return false;

            double distance = Vector3D.Distance(_worldLockedPosition, target.Position);
            if (distance < _worldLockedClosestDistance)
                _worldLockedClosestDistance = distance;

            // The receding check prevents an unusually large grid from ending a
            // fly-by instantly when its initial camera placement already exceeds
            // the normal exit radius.
            return distance >= exitDistance &&
                distance > _worldLockedClosestDistance + System.Math.Min(25.0, exitDistance * 0.05);
        }

        public void Clear()
        {
            _activeShot = ShotType.RearHighThreeQuarter;
            _hasActiveShot = false;
            _worldLockedPosition = Vector3D.Zero;
            _worldLockedClosestDistance = 0.0;
        }

        private static void ResolveFrame(CinematicTarget target, out Vector3D up,
            out Vector3D forward, out Vector3D right)
        {
            float interference;
            Vector3 gravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(target.Position, out interference);
            if (gravity.LengthSquared() > 0.0001f)
            {
                up = -Vector3D.Normalize(gravity);
                forward = target.Controller.WorldMatrix.Forward -
                    up * Vector3D.Dot(target.Controller.WorldMatrix.Forward, up);
                if (forward.LengthSquared() < 0.01)
                    forward = target.Controller.WorldMatrix.Up - up * Vector3D.Dot(target.Controller.WorldMatrix.Up, up);
                if (forward.LengthSquared() < 0.01)
                    forward = Vector3D.CalculatePerpendicularVector(up);
                forward.Normalize();
                right = Vector3D.Cross(forward, up);
                right.Normalize();
                return;
            }

            // In zero gravity the occupied controller defines the pilot's real
            // up/forward frame. A cockpit can be mounted in a grid orientation
            // that makes the grid axes cinematographically misleading.
            up = target.Controller.WorldMatrix.Up;
            forward = target.Controller.WorldMatrix.Forward;
            right = target.Controller.WorldMatrix.Right;
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
