using Sandbox.ModAPI;
using SECinematicCamera.Config;
using SECinematicCamera.Director;
using SECinematicCamera.Target;
using System;
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
        private readonly Random _random = new Random();
        private bool _chaseOffsetVariant;
        private double _chasePhase;
        private double _chaseRearYawDegrees;
        private Vector3D _chasePreviousVelocity;
        private Vector3D _chaseAcceleration;
        private bool _hasChaseVelocity;
        private Vector3D _chaseStableUp;
        private bool _hasChaseUp;

        public ShotSolver(CinematicConfig config)
        {
            _config = config;
        }

        public CameraPose Solve(CinematicTarget target, ShotType shot,
            double elapsedSeconds, double shotAgeSeconds)
        {
            ShotProfile profile = ShotProfile.Get(shot);
            Vector3D up;
            Vector3D forward;
            Vector3D right;
            bool gravityAligned;
            ResolveFrame(target, out up, out forward, out right, out gravityAligned);
            Vector3D lookAhead = (Vector3D)target.LinearVelocity * _config.LookAheadSeconds;
            double maximumLookAhead = System.Math.Max(0.5, target.Scale * 0.15);
            if (lookAhead.LengthSquared() > maximumLookAhead * maximumLookAhead)
                lookAhead = Vector3D.Normalize(lookAhead) * maximumLookAhead;

            Vector3D lookAt = target.Position + lookAhead;

            if (shot == ShotType.FrontMountedClose || shot == ShotType.RearMountedForward)
                return SolveMounted(target, shot);

            if (shot == ShotType.Chase)
                return SolveChase(target, up, forward, right, gravityAligned,
                    lookAt, elapsedSeconds, shotAgeSeconds);

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

        private CameraPose SolveChase(CinematicTarget target, Vector3D frameUp,
            Vector3D shipForward, Vector3D shipRight, bool gravityAligned,
            Vector3D lookAt, double elapsedSeconds, double shotAgeSeconds)
        {
            Vector3D velocity = target.LinearVelocity;
            double speed = velocity.Length();
            Vector3D trajectoryForward = shipForward;
            if (speed > 0.5)
            {
                Vector3D velocityDirection = velocity / speed;
                double velocityWeight = MathHelper.Clamp((speed - 2.0) / 20.0, 0.0, 1.0);
                trajectoryForward = shipForward * (1.0 - velocityWeight) +
                    velocityDirection * velocityWeight;
                if (trajectoryForward.LengthSquared() < 0.001)
                    trajectoryForward = velocityDirection;
                else
                    trajectoryForward.Normalize();
            }

            Vector3D chaseRight = Vector3D.Cross(trajectoryForward, frameUp);
            if (chaseRight.LengthSquared() < 0.001)
                chaseRight = shipRight;
            chaseRight.Normalize();

            UpdateChaseAcceleration(velocity, elapsedSeconds);
            double forwardAcceleration = Math.Max(0.0,
                Vector3D.Dot(_chaseAcceleration, trajectoryForward));
            double speedExtension = MathHelper.Clamp(speed / 100.0, 0.0, 1.0) * 0.70;
            double accelerationExtension = MathHelper.Clamp(forwardAcceleration / 25.0,
                0.0, 0.40);
            double distance = Math.Max(target.Scale * _config.Distance * 1.35,
                target.Scale * 0.95) * (1.0 + speedExtension + accelerationExtension);

            double sideDegrees;
            if (_chaseOffsetVariant)
            {
                // Two slow layers prevent an obvious pendulum loop while keeping
                // the camera on a continuous rear-left/center/rear-right arc.
                sideDegrees = Math.Sin(shotAgeSeconds * 0.31 + _chasePhase) * 11.0 +
                    Math.Sin(shotAgeSeconds * 0.13 + _chasePhase * 1.7) * 4.0;
            }
            else
            {
                sideDegrees = _chaseRearYawDegrees +
                    Math.Sin(shotAgeSeconds * 0.19 + _chasePhase) * 1.5;
            }

            double elevationDegrees = 11.0 +
                Math.Sin(shotAgeSeconds * 0.23 + _chasePhase * 0.7) * 4.0;
            sideDegrees = MathHelper.Clamp(sideDegrees *
                (_config.Side / CinematicConfig.DefaultSide), -35.0, 35.0);
            elevationDegrees = MathHelper.Clamp(elevationDegrees *
                (_config.Height / CinematicConfig.DefaultHeight), 3.0, 35.0);
            double sideOffset = Math.Tan(MathHelper.ToRadians((float)sideDegrees)) * distance;
            double heightOffset = Math.Tan(MathHelper.ToRadians((float)elevationDegrees)) * distance;
            Vector3D chaseUp = ResolveChaseUp(target, frameUp, trajectoryForward,
                gravityAligned, elapsedSeconds);
            Vector3D position = target.Position - trajectoryForward * distance +
                chaseRight * sideOffset + chaseUp * heightOffset;

            return new CameraPose(position, lookAt, chaseUp);
        }

        private void UpdateChaseAcceleration(Vector3D velocity, double elapsedSeconds)
        {
            if (!_hasChaseVelocity || elapsedSeconds <= 0.0001 || elapsedSeconds > 0.1)
            {
                _chasePreviousVelocity = velocity;
                _chaseAcceleration = Vector3D.Zero;
                _hasChaseVelocity = true;
                return;
            }

            Vector3D measured = (velocity - _chasePreviousVelocity) / elapsedSeconds;
            double amount = 1.0 - Math.Exp(-elapsedSeconds / 0.18);
            _chaseAcceleration = Vector3D.Lerp(_chaseAcceleration, measured, amount);
            _chasePreviousVelocity = velocity;
        }

        private Vector3D ResolveChaseUp(CinematicTarget target, Vector3D frameUp,
            Vector3D trajectoryForward, bool gravityAligned, double elapsedSeconds)
        {
            Vector3D shipUp = target.Controller.WorldMatrix.Up;
            shipUp -= trajectoryForward * Vector3D.Dot(shipUp, trajectoryForward);
            if (shipUp.LengthSquared() < 0.001)
                shipUp = frameUp;
            else
                shipUp.Normalize();

            if (gravityAligned)
            {
                // Preserve a readable horizon while carrying about 30% of the
                // vehicle's roll into the shot.
                Vector3D partialRollUp = Vector3D.Lerp(frameUp, shipUp, 0.30);
                if (partialRollUp.LengthSquared() > 0.001)
                    partialRollUp.Normalize();
                return partialRollUp;
            }

            if (!_hasChaseUp)
            {
                _chaseStableUp = shipUp;
                _hasChaseUp = true;
            }
            else if (elapsedSeconds > 0.0001)
            {
                // Space has no absolute horizon. A deliberately slow response
                // carries roll without snapping one-for-one with the cockpit.
                double amount = (1.0 - Math.Exp(-elapsedSeconds / 1.2)) * 0.35;
                _chaseStableUp = Vector3D.Lerp(_chaseStableUp, shipUp, amount);
                if (_chaseStableUp.LengthSquared() < 0.001)
                    _chaseStableUp = shipUp;
                else
                    _chaseStableUp.Normalize();
            }
            return _chaseStableUp;
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
            if (shot == ShotType.Chase)
            {
                _chaseOffsetVariant = _random.NextDouble() >= 0.5;
                _chasePhase = _random.NextDouble() * MathHelper.TwoPi;
                _chaseRearYawDegrees = (_random.NextDouble() * 2.0 - 1.0) * 4.5;
                _chasePreviousVelocity = target.LinearVelocity;
                _chaseAcceleration = Vector3D.Zero;
                _hasChaseVelocity = true;
                _chaseStableUp = Vector3D.Zero;
                _hasChaseUp = false;
            }
            if (ShotProfile.Get(shot).WorldLocked)
            {
                _hasActiveShot = false;
                _worldLockedPosition = Solve(target, shot, 0.0, 0.0).Position;
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

        public string GetShotName(ShotType shot)
        {
            if (shot == ShotType.Chase && _hasActiveShot && _activeShot == shot)
                return _chaseOffsetVariant ? "Chase Offset" : "Chase Rear";
            return ShotProfile.GetShortName(shot);
        }

        public void Clear()
        {
            _activeShot = ShotType.RearHighThreeQuarter;
            _hasActiveShot = false;
            _worldLockedPosition = Vector3D.Zero;
            _worldLockedClosestDistance = 0.0;
            _chaseOffsetVariant = false;
            _chasePhase = 0.0;
            _chaseRearYawDegrees = 0.0;
            _chasePreviousVelocity = Vector3D.Zero;
            _chaseAcceleration = Vector3D.Zero;
            _hasChaseVelocity = false;
            _chaseStableUp = Vector3D.Zero;
            _hasChaseUp = false;
        }

        private static void ResolveFrame(CinematicTarget target, out Vector3D up,
            out Vector3D forward, out Vector3D right, out bool gravityAligned)
        {
            float interference;
            Vector3 gravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(target.Position, out interference);
            if (gravity.LengthSquared() > 0.0001f)
            {
                gravityAligned = true;
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

            gravityAligned = false;

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
