using System.Text;
using CueSharp;
using Index = CueSharp.Index;

namespace InstrunetTestProject;

[TestClass]
public class Cue
{
    [TestMethod]
    public void Playground()
    {
        var sheet = new CueSheet();
        sheet.AddTrack(new Track
        {
            Comments = new string[]
            {
            },
            DataFile = new AudioFile
            {
                Filename = "a.mp3",
                Filetype = FileType.WAVE
            },
            Garbage = new string[]
            {
            },
            Indices = new Index[]
            {
            },
            ISRC = "",

            Songwriter = "A",
            Title = "Man",
            TrackDataType = DataType.AUDIO,
            TrackFlags = new Flags[]
            {
            },
            Performer = "A",
            PostGap = default,
            PreGap = default,
            TrackNumber = 0
        });
        sheet.AddTrack(new Track
        {
            Comments = new string[]
            {
            },
            DataFile = new AudioFile
            {
                Filename = "a.mp3",
                Filetype = FileType.WAVE
            },
            Garbage = new string[]
            {
            },
            Indices = new Index[]
            {
            },
            ISRC = "",

            Songwriter = "A",
            Title = "Man",
            TrackDataType = DataType.AUDIO,
            TrackFlags = new Flags[]
            {
            },
            Performer = "A",
            PostGap = default,
            PreGap = default,
            TrackNumber = 1
        });
        sheet.SaveCue("a.cue", Encoding.UTF8);
    }
}