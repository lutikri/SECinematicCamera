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
            ShotType.WorldLockedFlyBy
        };

        private readonly CinematicConfig _config;
        private readonly List<ShotType> _history = new List<ShotType>();
        private readonly Random _random = new Random();

        public ShotType CurrentShot { get; private set; }
        public float ShotAgeSeconds { get; private set; }
        public string LastChangeReason { get; private set; }

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
            if (!_config.Automatic || ShotAgeSeconds < _config.ShotDurationSeconds)
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
                if (candidate == CurrentShot || IsForbiddenMirrorCut(CurrentShot, candidate))
                    weight = 0.0;
                else if (_history.Contains(candidate))
                    weight *= 0.20;

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
                    return (0.55 + Math.Min(speed / 25.0, 1.5)) * (angular > 0.6 ? 0.30 : 1.0);
                case ShotType.FrontHighThreeQuarter:
                    return 0.75 + Math.Min(speed / 40.0, 0.8);
                case ShotType.OverheadDrift:
                    return 0.65 + Math.Min(angular * 2.5, 2.0);
                case ShotType.WorldLockedFlyBy:
                    return speed < 3.0 ? 0.10 : 0.40 + Math.Min(speed / 20.0, 2.0);
                default:
                    return 1.35;
            }
        }

        private static bool IsForbiddenMirrorCut(ShotType from, ShotType to)
        {
            return (from == ShotType.SideHighLeft && to == ShotType.SideHighRight) ||
                   (from == ShotType.SideHighRight && to == ShotType.SideHighLeft);
        }

        private void Remember(ShotType shot)
        {
            _history.Add(shot);
            while (_history.Count > 3)
                _history.RemoveAt(0);
        }
    }
}
