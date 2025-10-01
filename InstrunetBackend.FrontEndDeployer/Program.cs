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
    var @operator = new Signature("AXCWG Bot", "xiey0@qq.com", DateTimeOffset.Now);

    repo.MergeFetchedRefs(@operator, new MergeOptions()
    {
        FailOnConflict = true
    });
    var result = Commands.Pull(repo, @operator, new PullOptions());
    if (result.Status == MergeStatus.FastForward)
    {
        var p = new Process();
        p.StartInfo.FileName = "npm";
        p.StartInfo.Arguments = "run build";
        p.Start();
        p.WaitForExit(); 
        p.Dispose();
        File.Copy("./web.config", "dist/web.config");
    }
}