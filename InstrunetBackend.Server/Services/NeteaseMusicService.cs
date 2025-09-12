using System.Diagnostics;

namespace InstrunetBackend.Server.Services;

public class NeteaseMusicService
{
    public Process? Process { get; set;  }

    public NeteaseMusicService()
    {
        Process = new Process();
        Process.StartInfo.FileName = "npx";
        Process.StartInfo.Arguments = "NeteaseCloudMusicApi@latest";
        Process.StartInfo.EnvironmentVariables.Add("PORT", "3958");
        Process.Start(); 
    }
}