using System.Text.Json.Serialization;

namespace InstrunetBackend.Server.IndependantModels.HttpPayload
{
    public class PlaylistUploadContext
    {
        public class Tmb
        {
            [JsonPropertyName("type")] public string Type = "Buffer";
            [JsonPropertyName("data")] public int[]? Data { get; set; }
        }

        [JsonPropertyName("playlistuuid")] public string? Playlistuuid { get; set; }
        [JsonPropertyName("private")] public bool Private { get; set; }
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("tmb")] public Tmb? TmbInstance { get; set; }
        [JsonPropertyName("content")] public string[]? Content { get; set; }
    }
}
