using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using FFMpegCore;
using FFMpegCore.Pipes;
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
        /// <summary>
        /// Applies a pitch shift to the audio data from the specified stream and returns the result as a new
        /// MP3-formatted memory stream.
        /// </summary>
        /// <remarks>The input stream's position is reset to its original value after processing. The
        /// method processes the entire audio content synchronously and may be resource-intensive for large
        /// files.</remarks>
        /// <param name="file">The input stream containing MP3 audio data. The stream must be readable and positioned at the start of the
        /// audio content.</param>
        /// <param name="pitch">The number of semitones to shift the pitch. Positive values increase the pitch; negative values decrease it.
        /// Fractional values are supported.</param>
        /// <returns>A memory stream containing the pitch-shifted audio in MP3 format. The returned stream is positioned at the
        /// beginning.</returns>
        public static MemoryStream ToPitched(this Stream file, double pitch)
        {
            try
            {
                using var reader = new Mp3FileReaderBase(file, wf => new Mp3FrameDecompressor(wf));
                var p = new SmbPitchShiftingSampleProvider(reader.ToSampleProvider());

                p.PitchFactor = (float)Math.Pow(Math.Pow(2, 1.0 / 12), pitch);



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
                FFMpegArguments.FromPipeInput(new StreamPipeSource(memoryStream), o => o.WithAudioCodec("pcm_f32le")).OutputToPipe(new StreamPipeSink(outputMp3Stream), o => o.WithAudioCodec("mp3").ForceFormat("mp3").WithAudioBitrate(320000)).NotifyOnError(i => Console.WriteLine(i)).NotifyOnOutput(Console.WriteLine).WithLogLevel(FFMpegCore.Enums.FFMpegLogLevel.Warning).ProcessSynchronously();
                return outputMp3Stream;
            }
            finally
            {
                file.Position = 0; 
            }
            
        }
        /// <summary>
        /// Applies a pitch shift to the audio data from the specified stream and returns the result as a new
        /// MP3-formatted memory stream.
        /// </summary>
        /// <remarks>The input stream's position is reset to its original value after processing. The
        /// method processes the entire audio content synchronously and may be resource-intensive for large
        /// files.</remarks>
        /// <param name="file">The byte array containing the MP3 audio file data to be processed. Cannot be null.</param>
        /// <param name="pitch">The amount of semitones by which to adjust the pitch, expressed as a multiplier. Values greater than 1.0 increase the
        /// pitch; values less than 1.0 decrease it.</param>
        /// <returns>A MemoryStream containing the audio data with the adjusted pitch.</returns>
        public static MemoryStream ToPitched(this byte[] file, double pitch)
        {
            var mStream = new MemoryStream(file);
            var mStreamProcessed = ToPitched(mStream, pitch);
            mStream.Dispose();
            return mStreamProcessed;
        }
    }
}
