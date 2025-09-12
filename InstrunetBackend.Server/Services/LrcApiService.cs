using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace InstrunetBackend.Server.Services;

public class LrcApiService
{
    public Process? Process { get; set; }

    public LrcApiService()
    {
        Console.WriteLine("Extracting LrcApi Libraries");
        Directory.CreateDirectory(Program.LibraryCommon + "lrapi/");
        var assembly = Assembly.GetExecutingAssembly();
        Stream? stream = null;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                stream = assembly.GetManifestResourceStream("InstrunetBackend.Server.lib.lrcapi.lrcapi-Linux-aarch64"); 
            }
            else if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
            {
                stream = assembly.GetManifestResourceStream("InstrunetBackend.Server.lib.lrcapi.lrcapi-Linux-x86_64");
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            stream = assembly.GetManifestResourceStream("InstrunetBackend.Server.lib.lrcapi.lrcapi-win.exe");
        }

        if (stream == null)
        {
            Console.WriteLine("LrcApi not supported on this platform. ");
            return; 
        }
        var fStream = new FileStream (Program.LibraryCommon + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "lrcapi/lrcapi.exe" : "lrcapi/lrcapi"), RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? new()
        {
            Access = FileAccess.ReadWrite, Mode = FileMode.CreateNew
        } : new FileStreamOptions()
        {
            Access = FileAccess.ReadWrite,
            Mode = FileMode.CreateNew, 
            UnixCreateMode = UnixFileMode.GroupExecute | UnixFileMode.OtherExecute | UnixFileMode.UserExecute
        });
        stream.CopyTo(fStream);
        stream.Dispose();
        fStream.Dispose();
        Process = new();
        Process.StartInfo.FileName = Program.LibraryCommon +
                                     (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                                         ? "lrcapi/lrcapi.exe"
                                         : "lrcapi/lrcapi");
        Process.Start(); 
    }
}