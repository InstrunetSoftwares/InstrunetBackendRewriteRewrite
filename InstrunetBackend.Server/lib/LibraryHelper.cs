using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using WebPWrapper.Encoder;

namespace InstrunetBackend.Server.lib
{
    public static class LibraryHelper
    {
        /// <summary>
        /// Creates a new instance of <see cref="WebPEncoderBuilder"/> configured for the current operating system and
        /// architecture, if supported.
        /// </summary>
        /// <remarks>This method determines the appropriate WebP encoder executable based on the operating
        /// system and process architecture. If the platform or architecture is not supported, the method returns <see
        /// langword="null"/>.</remarks>
        /// <returns>A <see cref="WebPEncoderBuilder"/> instance if the current platform and architecture are supported;
        /// otherwise, <see langword="null"/>.</returns>
        [Pure]
        public static WebPEncoderBuilder? CreateWebPEncoderBuilder()
        {
            WebPEncoderBuilder? builder = null;
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                    builder = new WebPEncoderBuilder(Program.CWebP + "libwebp-1.6.0-windows-x64/bin/cwebp.exe");
                    break;
                case PlatformID.Unix:
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                        {
                            builder = new WebPEncoderBuilder(Program.CWebP + "libwebp-1.6.0-mac-arm64/bin/cwebp");
                            break;
                        }

                        builder = new WebPEncoderBuilder(Program.CWebP + "libwebp-1.6.0-mac-x86-64/bin/cwebp");
                        break;
                    }

                    Console.WriteLine("No cwebp for you. ");
                    break;


                case PlatformID.Other:
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                        {
                            builder = new WebPEncoderBuilder(
                                Program.CWebP + "libwebp-1.6.0-linux-aarch64/bin/cwebp");
                            break;
                        }

                        builder = new WebPEncoderBuilder(Program.CWebP + "libwebp-1.6.0-linux-x86-64/bin/cwebp");
                        break;
                    }

                    Console.WriteLine("No cwebp for you. ");
                    break;
            }
            return builder; 
        }
    }
}
