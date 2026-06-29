using Brutal.Logging;
using System.Diagnostics;

namespace Custom_Start_Page_Tools
{
    internal static class StarMapRestarter
    {
        private static readonly string? StarMapPath = Process.GetCurrentProcess().MainModule?.FileName;
        internal static void RestartStarMap()
        {
            if (StarMapPath == null)
            {
                DefaultCategory.Log.Debug("Custom Start Page Tools - Could not determine StarMap path.");
                return;
            }

            Process? starMapRestart = Process.Start(new ProcessStartInfo
            {
                FileName = StarMapPath,
                WorkingDirectory = Path.GetDirectoryName(StarMapPath),
                UseShellExecute = false
            });

            if (starMapRestart != null)
            {
                Environment.Exit(0);
            }
            else
            {
                DefaultCategory.Log.Info("Custom Start Page Tools - StarMap restart failed.");
            }  
        }
    }
}
