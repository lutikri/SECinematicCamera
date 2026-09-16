using SECinematicCamera.Config;
using SECinematicCamera.Target;
using System;
using System.Collections.Generic;

namespace SECinematicCamera.Director
{
    public sealed class CameraDirector
    {
        private static readonly ShotType[] AvailableShots =
        {
            ShotType.RearHighThreeQuarter,
            ShotType.SideHighLeft,
            ShotType.SideHighRight,
            ShotType.FrontHighThreeQuarter,
            ShotType.OverheadDrift,
            ShotType.WorldLockedFlyBy,
            ShotType.FrontMountedClose,
            ShotType.RearMountedForward,
            ShotType.DistantTopSide
        };

        private readonly CinematicConfig _config;
        private readonly List<ShotType> _history = new List<ShotType>();
        private readonly Random _random = new Random();

        public ShotType CurrentShot { get; private set; }
        public float ShotAgeSeconds { get; private set; }
        public string LastChangeReason { get; private set; }
        public float CurrentDurationSeconds
        {
            get
            {
                float duration = _config.ShotDurationSeconds *
                    (float)ShotProfile.Get(CurrentShot).DurationScale;
                return CurrentShot == ShotType.WorldLockedFlyBy
                    ? System.Math.Min(6f, duration)
                    : duration;
            }
        }

        public CameraDirector(CinematicConfig config)
        {
            _config = config;
        }

        public void Reset(CinematicTarget target)
        {
            _history.Clear();
            CurrentShot = ShotType.RearHighThreeQuarter;
            ShotAgeSeconds = 0f;
            LastChangeReason = "Safe initial shot";
        }

        public bool Update(CinematicTarget target, float elapsedSeconds)
        {
            ShotAgeSeconds += elapsedSeconds;
            if (!_config.Automatic || ShotAgeSeconds < CurrentDurationSeconds)
                return false;

            return ChangeTo(ChooseNext(target), "Duration elapsed; motion-weighted automatic choice");
        }

        public void SelectManual(ShotType shot)
        {
            ChangeTo(shot, "Manual selection");
        }

        public bool ForceNext(CinematicTarget target)
        {
            return ChangeTo(ChooseNext(target), "Forced next; motion-weighted choice");
        }

        public bool RequestNext(CinematicTarget target, string reason)
        {
            return ChangeTo(ChooseNext(target), reason);
        }

        public void SelectNextManual()
        {
            int index = System.Array.IndexOf(AvailableShots, CurrentShot);
            ShotType next = AvailableShots[(index + 1 + AvailableShots.Length) % AvailableShots.Length];
            ChangeTo(next, "Manual hotkey cycle");
        }

        public void RestartCurrent(string reason)
        {
            ShotAgeSeconds = 0f;
            LastChangeReason = reason;
        }

        public void Clear()
        {
            _history.Clear();
            CurrentShot = ShotType.RearHighThreeQuarter;
            ShotAgeSeconds = 0f;
            LastChangeReason = null;
        }

        private bool ChangeTo(ShotType shot, string reason)
        {
            if (shot == CurrentShot)
            {
                ShotAgeSeconds = 0f;
                LastChangeReason = reason + "; safe fallback retained";
                return false;
            }

            Remember(CurrentShot);
            CurrentShot = shot;
            ShotAgeSeconds = 0f;
            LastChangeReason = reason;
            return true;
        }

        private ShotType ChooseNext(CinematicTarget target)
        {
            double[] weights = new double[AvailableShots.Length];
            double total = 0.0;
            for (int index = 0; index < AvailableShots.Length; index++)
            {
                ShotType candidate = AvailableShots[index];
                double weight = Suitability(candidate, target);
                if (candidate == CurrentShot || AreVisuallySimilar(CurrentShot, candidate) ||
                    IsForbiddenMirrorCut(CurrentShot, candidate) ||
                    IsForbiddenMountedFlip(CurrentShot, candidate))
                    weight = 0.0;
                else if (_history.Contains(candidate))
                    weight *= 0.12;

                weights[index] = weight;
                total += weight;
            }

            if (total <= 0.0001)
                return ShotType.RearHighThreeQuarter;

            double choice = _random.NextDouble() * total;
            for (int index = 0; index < AvailableShots.Length; index++)
            {
                choice -= weights[index];
                if (choice <= 0.0 && weights[index] > 0.0)
                    return AvailableShots[index];
            }

            return ShotType.RearHighThreeQuarter;
        }

        private static double Suitability(ShotType shot, CinematicTarget target)
        {
            double speed = target.Speed;
            double angular = target.AngularSpeed;
            switch (shot)
            {
                case ShotType.SideHighLeft:
                case ShotType.SideHighRight:
                    return (0.10 + Math.Min(speed / 100.0, 0.18)) * (angular > 0.6 ? 0.25 : 1.0);
                case ShotType.FrontHighThreeQuarter:
                    return 0.16 + Math.Min(speed / 160.0, 0.14);
                case ShotType.OverheadDrift:
                    return 0.35 + Math.Min(angular * 1.4, 1.2);
                case ShotType.WorldLockedFlyBy:
                    return speed < 5.0 ? 0.08 : 1.15 + Math.Min(speed / 45.0, 1.5);
                case ShotType.FrontMountedClose:
                    return speed < 4.0 ? 0.12 : 0.75 + Math.Min(speed / 90.0, 0.65);
                case ShotType.RearMountedForward:
                    return speed < 4.0 ? 0.10 : 0.65 + Math.Min(speed / 100.0, 0.55);
                case ShotType.DistantTopSide:
                    return 0.80 + Math.Min(speed / 140.0, 0.45);
                default:
                    return 0.45;
            }
        }

        private static bool IsOrbitShot(ShotType shot)
        {
            return shot == ShotType.RearHighThreeQuarter ||
                   shot == ShotType.SideHighLeft ||
                   shot == ShotType.SideHighRight ||
                   shot == ShotType.FrontHighThreeQuarter;
        }

        private static bool AreVisuallySimilar(ShotType from, ShotType to)
        {
            if (IsOrbitShot(from) && IsOrbitShot(to))
                return true;

            return (from == ShotType.RearHighThreeQuarter && to == ShotType.RearMountedForward) ||
                   (from == ShotType.RearMountedForward && to == ShotType.RearHighThreeQuarter) ||
                   (from == ShotType.FrontHighThreeQuarter && to == ShotType.FrontMountedClose) ||
                   (from == ShotType.FrontMountedClose && to == ShotType.FrontHighThreeQuarter);
        }

        private static bool IsForbiddenMirrorCut(ShotType from, ShotType to)
        {
            return (from == ShotType.SideHighLeft && to == ShotType.SideHighRight) ||
                   (from == ShotType.SideHighRight && to == ShotType.SideHighLeft);
        }

        private static bool IsForbiddenMountedFlip(ShotType from, ShotType to)
        {
            return (from == ShotType.FrontMountedClose && to == ShotType.RearMountedForward) ||
                   (from == ShotType.RearMountedForward && to == ShotType.FrontMountedClose);
        }

        private void Remember(ShotType shot)
        {
            _history.Add(shot);
            while (_history.Count > 3)
                _history.RemoveAt(0);
        }
    }
}
