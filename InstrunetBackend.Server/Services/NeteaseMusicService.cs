using System.Diagnostics;

namespace InstrunetBackend.Server.Services;

public class NeteaseMusicService
{
    public NeteaseMusicService()
    {
        if (OperatingSystem.IsWindows())
        {
            Process = new Process();
            Process.StartInfo.FileName = "cmd";
            Process.StartInfo.Arguments = "/c npx --yes NeteaseCloudMusicApi@latest";
            Process.StartInfo.EnvironmentVariables.Add("PORT", "3958");
            Process.Start();
            return;
        }

        Process = new Process();
        Process.StartInfo.UseShellExecute = false;
        Process.StartInfo.FileName = "bash";
        Process.StartInfo.Arguments = "-c \"source ~/.bashrc && pnpm dlx NeteaseCloudMusicApi@latest";
        Process.StartInfo.EnvironmentVariables.Add("PORT", "3958");
        Process.StartInfo.RedirectStandardInput = false;
        Process.Start();
        Task.Run(async () =>
        {
            while (true)
            {
                await Process.WaitForExitAsync();
                Process.Start();
            }
            // ReSharper disable once FunctionNeverReturns
        });
    }

    public Process? Process { get; set; }
}