using System.Diagnostics;

namespace UnannoyifyLeague;

internal static class StartupHandler
{
    public static string UnannoyifyLeagueTitle => "UnannoyifyLeague " + Utils.UnannoyifyLeagueVersion;

    [STAThread]
    public static async Task Main()
    {
        try
        {
            await StartUnannoyifyLeagueAsync();
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex);
            // Show some kind of message so that UnannoyifyLeague doesn't just disappear.
            MessageBox.Show(
                "UnannoyifyLeague encountered an error and couldn't properly initialize itself. " +
                "Please contact the creator through GitHub (https://github.com/leoliz/UnannoyifyLeague) or Discord.\n\n" + ex,
                UnannoyifyLeagueTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error,
                MessageBoxDefaultButton.Button1
            );
        }
    }

    private static async Task StartUnannoyifyLeagueAsync()
    {
        if (Utils.IsClientRunning())
        {
            Utils.KillProcesses();
            await Task.Delay(1500);
        }

        var riotClientPath = Utils.GetRiotClientPath();

        if (riotClientPath is null)
        {
            MessageBox.Show(
                "Unable to find the path to the Riot Client. Usually this can be resolved by launching any Riot Games game once, then launching again. " +
                "If this does not resolve the issue, please file a bug report through GitHub (https://github.com/leoliz/UnannoyifyLeague) or Discord.",
                UnannoyifyLeagueTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error,
                MessageBoxDefaultButton.Button1
            );

            return;
        }

        var startArgs = new ProcessStartInfo
        {
            FileName = riotClientPath,
            Arguments = "--launch-product=league_of_legends --launch-patchline=live"
        };

        Trace.WriteLine($"About to launch Riot Client with parameters:\n{startArgs.Arguments}");
        var riotClient = Process.Start(startArgs);
        if (riotClient is not null)
            await MonitorLeagueCloseAsync(riotClient);
    }

    private static async Task MonitorLeagueCloseAsync(Process riotClientProcess)
    {
        while (true)
        {
            var leagueClient = Utils.GetLeagueClientProcess();
            if (leagueClient is not null)
            {
                Trace.WriteLine("League client detected, waiting for it to close.");
                await Task.Delay(10000);
                Utils.CloseRiotClientWindow();
                await Task.Delay(3200);
                await Task.Run(leagueClient.WaitForExit);
                await Task.Delay(1200);
                Utils.KillProcesses();
                Application.Exit();
                return;
            }

            riotClientProcess.Refresh();
            if (riotClientProcess.HasExited)
            {
                Application.Exit();
                return;
            }

            await Task.Delay(10000);
        }
    }
}