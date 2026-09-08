using Sandbox.ModAPI;

namespace SECinematicCamera.Debug
{
    public sealed class CineLogger
    {
        private const string Sender = "CinematicCamera";

        public bool DebugEnabled { get; set; }

        public void Info(string message)
        {
            if (MyAPIGateway.Utilities != null)
                MyAPIGateway.Utilities.ShowMessage(Sender, message);
        }

        public void Debug(string message)
        {
            if (DebugEnabled)
                Info(message);
        }
    }
}
