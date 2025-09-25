using System.Text.Json.Serialization;

namespace InstrunetBackend.Server.IndependantModels.HttpPayload
{
    public class PlaylistUploadContext
    {
        [JsonPropertyName("playlistuuid")]
        public required string PlaylistUuid { get; set; }
        public required bool Private { get; set; }
        public required string Title { get; set; }
        public required string[] Content { get; set; }
    }
    public class PlaylistUploadThumbnailContext
    {
        [JsonPropertyName("playlistuuid")]
        public required string PlaylistUuid { get; set;  }
        public required string DataUri { get; set;  }
    }
}