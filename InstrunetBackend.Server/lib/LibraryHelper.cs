using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using FFMpegCore;
using FFMpegCore.Pipes;
using NAudio.Lame;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NLayer.NAudioSupport;
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
        public static MemoryStream ToPitched(this Stream file, double pitch)
        {
            using var reader = new Mp3FileReaderBase(file, wf => new Mp3FrameDecompressor(wf));
            var p = new SmbPitchShiftingSampleProvider(reader.ToSampleProvider());

            p.PitchFactor = (float)Math.Pow(Math.Pow(2, 1.0 / 12), pitch * 2);



            using var memoryStream = new MemoryStream();
            using var wave = new WaveFileWriter(memoryStream, p.WaveFormat);

            float[] buffer = new float[1024];
            int read;
            while ((read = p.Read(buffer, 0, buffer.Length)) > 0)
            {
                wave.WriteSamples(buffer, 0, read);
            }
            memoryStream.Position = 0;
            var outputMp3Stream = new MemoryStream();
            FFMpegArguments.FromPipeInput(new StreamPipeSource(memoryStream), o => o.WithAudioCodec("pcm_f32le")).OutputToPipe(new StreamPipeSink(outputMp3Stream), o => o.WithAudioCodec("mp3").ForceFormat("mp3")).NotifyOnError(i => Console.WriteLine(i)).NotifyOnOutput(Console.WriteLine).WithLogLevel(FFMpegCore.Enums.FFMpegLogLevel.Debug).ProcessSynchronously();
            return outputMp3Stream;
        }

        public static MemoryStream ToPitched(this byte[] file, double pitch)
        {
            var mStream = new MemoryStream(file);
            var mStreamProcessed = ToPitched(mStream, pitch);
            mStream.Dispose();
            return mStreamProcessed;
        }
    }
}
