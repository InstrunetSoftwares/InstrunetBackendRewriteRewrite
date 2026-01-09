using System.Text.Json.Serialization;

namespace InstrunetBackend.Server.IndependantModels.HttpPayload
{
    public class PlaylistUploadContext
    {
        [JsonPropertyName("playlistuuid")]
        public required string PlaylistUuid { get; set; }
        public required bool Private { get; set; }
        public required string Title { get; set; }
        /// <summary>
        /// Definitely needs to have a uuid field. 
        /// </summary>
        public required PlaylistInfo[] Content { get; set; }

       public class PlaylistInfo
        {
            public string? Uuid { get; set; }
            public string? SongName { get; set; }
            public string? AlbumName { get; set; }
            public string? Artist { get; set; }
            public int Kind { get; set; }
        }
    }
    public class PlaylistUploadThumbnailContext
    {
        [JsonPropertyName("playlistuuid")]
        public required string PlaylistUuid { get; set;  }
        public required string DataUri { get; set;  }
    }
}