using System.Text.Json.Serialization;

namespace InstrunetBackend.Server.IndependantModels.HttpPayload
{
    public class RemovePlaylistPayload
    {
       
            [JsonPropertyName("playlistuuid")]
            public string? Playlistuuid { get; set; }
        
    }
}
