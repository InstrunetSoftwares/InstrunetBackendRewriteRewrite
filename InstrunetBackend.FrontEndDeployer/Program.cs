using System.Diagnostics;
using LibGit2Sharp;


using var repo = new Repository(".");

if (!Directory.Exists(".git"))
{
    Console.WriteLine("No git repo found. ");
    return;
}

_ = Task.Run(() =>
{
    while (true)
    {
        var command = Console.ReadLine();
        switch (command?.Trim())
        {
            case "exec":
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

                break; 
            default:
                Console.WriteLine("Invalid command. Try again.");
                break; 
        }
    }
    // ReSharper disable once FunctionNeverReturns
}); 

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
            using var npm_i = new Process();
            npm_i.StartInfo.UseShellExecute = true;
            npm_i.StartInfo.FileName = "npm";
            npm_i.StartInfo.Arguments = "i"; 
            npm_i.Start();
            npm_i.WaitForExit(); 
            using var p = new Process();
            p.StartInfo.UseShellExecute = true; 
            p.StartInfo.FileName = "npm";
            p.StartInfo.Arguments = "run build";
            p.Start();
            p.WaitForExit();
            Console.WriteLine("Copying files...."); 
            File.Copy("./web.config", "dist/web.config", true);
            Console.WriteLine("Done copying files. "); 
        }
    }
    catch (Exception e)
    {
        Console.WriteLine(e); 
    }
   
    Console.WriteLine("Waiting for another 15 min. "); 
    Task.Delay(TimeSpan.FromMinutes(15)).GetAwaiter().GetResult(); 
}