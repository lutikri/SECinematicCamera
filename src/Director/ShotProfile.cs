namespace SECinematicCamera.Director
{
    public sealed class ShotProfile
    {
        public ShotType Type { get; private set; }
        public string Name { get; private set; }

        public ShotProfile(ShotType type, string name)
        {
            Type = type;
            Name = name;
        }
    }
}
