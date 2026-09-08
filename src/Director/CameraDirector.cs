using SECinematicCamera.Target;

namespace SECinematicCamera.Director
{
    public sealed class CameraDirector
    {
        public ShotType CurrentShot { get; private set; }
        public float ShotAgeSeconds { get; private set; }
        public string LastChangeReason { get; private set; }

        public void Reset(CinematicTarget target)
        {
            CurrentShot = ShotType.RearHighThreeQuarter;
            ShotAgeSeconds = 0f;
            LastChangeReason = "Safe initial shot";
        }

        public bool Update(CinematicTarget target, float elapsedSeconds)
        {
            ShotAgeSeconds += elapsedSeconds;
            return false;
        }

        public void SelectManual(ShotType shot)
        {
            CurrentShot = shot;
            ShotAgeSeconds = 0f;
            LastChangeReason = "Manual selection";
        }

        public void Clear()
        {
            CurrentShot = ShotType.RearHighThreeQuarter;
            ShotAgeSeconds = 0f;
            LastChangeReason = null;
        }
    }
}
