using System.Text;
using CueSharp;
using Index = CueSharp.Index;

namespace InstrunetTestProject;

[TestClass]
public partial class Cue
{
    [TestMethod]
    public void Playground()
    {
        var sheet = new CueSharp.CueSheet();
        sheet.AddTrack(new()
        {

            DataFile = new AudioFile
            {
                Filename = "a.mp3",
                Filetype = FileType.WAVE
            },
            ISRC = null,
            Performer = null,
            Songwriter = "A",
            Title = "Man",
            TrackDataType = DataType.AUDIO,
            
            TrackNumber = 0
        });
        sheet.SaveCue("a.cue", Encoding.UTF8);
    }
}