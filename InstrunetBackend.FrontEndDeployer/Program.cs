using System.Diagnostics;
using LibGit2Sharp;

using var repo = new Repository(".");
if (!Directory.Exists(".git"))
{
    Console.WriteLine("No git repo found. ");
    return;
}

while (true)
{
    Console.WriteLine("Enabling operation..."); 
    var @operator = new Signature("AXCWG Bot", "xiey0@qq.com", DateTimeOffset.Now);
    Console.WriteLine("Now pulling...");
    try
    {
        var result = Commands.Pull(repo, @operator, new PullOptions());
        if (result.Status == MergeStatus.FastForward)
        {
            Console.WriteLine("Fast forwarding..."); 
            var p = new Process();
            p.StartInfo.UseShellExecute = true; 
            p.StartInfo.FileName = "npm";
            p.StartInfo.Arguments = "run build";
            p.Start();
            p.WaitForExit();
            p.Dispose();
            Console.WriteLine("Copying files...."); 
            File.Copy("./web.config", "dist/web.config");
            Console.WriteLine("Done copying files. "); 
        }
    }
    catch (Exception e)
    {
        Console.WriteLine(e); 
    }
   
    Console.WriteLine("Waiting for another 15 sec. "); 
    Task.Delay(TimeSpan.FromSeconds(15)).GetAwaiter().GetResult(); 
}