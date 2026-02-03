using System.Diagnostics;
using System.Runtime.InteropServices;

namespace InstrunetBackend.Server.Services;

public class NeteaseMusicService
{
    public Process? Process { get; set;  }

    public NeteaseMusicService()
    {
        if(OperatingSystem.IsWindows())
        {
            Process = new Process();
            Process.StartInfo.FileName = "cmd"; 
            Process.StartInfo.Arguments = "/c npx --yes NeteaseCloudMusicApi@latest";
            Process.StartInfo.EnvironmentVariables.Add("PORT", "3958");
            Process.Start();
            return; 
        }
        Process = new Process();
        Process.StartInfo.FileName = "npx";
        Process.StartInfo.Arguments = "--yes NeteaseCloudMusicApi@latest";
        Process.StartInfo.EnvironmentVariables.Add("PORT", "3958");
        Process.Start(); 
    }
}