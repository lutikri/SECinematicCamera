using Sandbox.ModAPI;

namespace SECinematicCamera.Debug
{
    public sealed class CineLogger
    {
        public bool DebugEnabled { get; set; }

        public void Info(string message)
        {
            // UI services may disappear before session components during Alt+F4
            // or world unload. Logging must never turn safe shutdown into a mod
            // exception reported by Space Engineers.
            try
            {
                if (MyAPIGateway.Utilities != null)
                    MyAPIGateway.Utilities.ShowNotification(message, 2200, "White");
            }
            catch (System.Exception) { }
        }

        public void Debug(string message)
        {
            if (DebugEnabled)
                Info(message);
        }
    }
}
