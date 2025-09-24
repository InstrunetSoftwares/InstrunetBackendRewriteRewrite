using System.Diagnostics;
using NAudio.Dsp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NLayer.NAudioSupport;

namespace InstrunetTestProject;

[TestClass]
public sealed class Test1
{
    [TestMethod]
    public void TestMethod1()
    {
        dynamic text = new HttpClient();
        Console.WriteLine(text.GetType());
    }
    [TestMethod]
    public void LulTest()
    {
        byte[] a = [2, 34, 4, 56, 45, 254];
        var mStream = new MemoryStream(a);
        mStream.Dispose();
        Console.WriteLine(a[2]); 
        Assert.IsTrue(a is not null); 
    }

    [TestMethod]
    public void Test()
    {
        // TODO I Dont understand. 
        Environment.CurrentDirectory = "../../../"; 
        Console.WriteLine(Environment.CurrentDirectory);
        MemoryStream Pitch(Stream file, double pitch)
        {
            using var reader = new Mp3FileReaderBase(file, wf => new Mp3FrameDecompressor(wf));
            var p = new SmbPitchShiftingSampleProvider(reader.ToSampleProvider());
            
                p.PitchFactor = (float)Math.Pow(Math.Pow(2, 1.0 / 12), pitch*2);
                
            
            
            var memoryStream = new MemoryStream(); 
            var wave = new WaveFileWriter(memoryStream, p.WaveFormat);
            
            float[] buffer = new float[1024];
            int read;
            while ((read = p.Read(buffer, 0, buffer.Length)) > 0)
            {
                wave.WriteSamples(buffer, 0, read);
            }

            memoryStream.Position = 0; 
            return memoryStream; 
        }

        var file = new FileStream("music.mp3", FileMode.Open);
        var up1 = Pitch(file, 1.0);
        file.Position = 0; 
        var down1 = Pitch(file, -1.0);
        var fileStreamOut1 = File.Create("out+1.wav"); 
        up1.CopyTo(fileStreamOut1);
        var fileStreamOut2 = File.Create("out+2.wav"); 
        down1.CopyTo(fileStreamOut2);




    }
}