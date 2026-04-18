using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace UnannoyifyLeague;

internal static class Utils
{
    internal static string UnannoyifyLeagueVersion
    {
        get
        {
            var version = Assembly.GetEntryAssembly()?.GetName().Version;
            if (version is null)
                return "v0.0.0";
            return "v" + version.Major + "." + version.Minor + "." + version.Build;
        }
    }

    public static IEnumerable<Process> GetProcesses()
    {
        var processNames = new[]
        {
            "LeagueClient",
            "LeagueClientUx",
            "LeagueClientUxRender",
            "RiotClientServices",
            "RiotClientUx",
            "RiotClientUxRender",
            "RiotClientCrashHandler",
            "RiotClientCrashHandler64"
        };

        return processNames.SelectMany(Process.GetProcessesByName)
            .Where(process => process.Id != Process.GetCurrentProcess().Id)
            .GroupBy(process => process.Id)
            .Select(group => group.First());
    }

    public static void CloseRiotClientWindow()
    {
        var processNames = new[]
        {
            "Riot Client"
        };

        var processes = processNames.SelectMany(Process.GetProcessesByName);
        foreach (var process in processes)
        {
            if (process.MainWindowTitle != "")
                process.CloseMainWindow();
        }
    }

    public static Process? GetLeagueClientProcess()
    {
        var leagueNames = new[] { "LeagueClient" };
        return leagueNames.SelectMany(Process.GetProcessesByName).FirstOrDefault();
    }

    public static bool IsClientRunning() => GetProcesses().Any();

    public static void KillProcesses()
    {
        try
        {
            foreach (var process in GetProcesses())
            {
                process.Refresh();
                if (process.HasExited)
                    continue;
                process.Kill();
                process.WaitForExit();
            }
        }
        catch (Win32Exception ex)
        {
            // thank you C# and your horrible win32 ecosystem integration, I have no clue if this is correct
            if (ex.NativeErrorCode == -2147467259 || ex.ErrorCode == -2147467259 || ex.ErrorCode == 5 ||
                ex.NativeErrorCode == 5)
            {
                MessageBox.Show(
                    "UnannoyifyLeague could not stop existing Riot processes because it does not have the right permissions. Please relaunch this application as an administrator and try again.",
                    StartupHandler.UnannoyifyLeagueTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error,
                    MessageBoxDefaultButton.Button1
                );
                Environment.Exit(0);
            }

            throw ex;
        }
    }

    public static string? GetRiotClientPath()
    {
        var installPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Riot Games/RiotClientInstalls.json");
        if (!File.Exists(installPath))
            return null;

        try
        {
            var installJson = File.ReadAllText(installPath);
            const string marker = "\"rc_default\":";
            var markerIndex = installJson.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex < 0)
                return null;

            var firstQuote = installJson.IndexOf('"', markerIndex + marker.Length);
            if (firstQuote < 0)
                return null;

            var secondQuote = installJson.IndexOf('"', firstQuote + 1);
            if (secondQuote < 0)
                return null;

            var path = installJson.Substring(firstQuote + 1, secondQuote - firstQuote - 1).Replace("\\\\", "\\");
            return File.Exists(path) ? path : null;
        }
        catch
        {
            return null;
        }
    }
}
