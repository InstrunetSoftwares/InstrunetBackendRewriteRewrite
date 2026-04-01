using TagLib.Riff;

namespace InstrunetBackend.Server;

public class SongImageCache
{
    public ICollection<CacheEntity> ImageCacheCollection { get; set; } =  [];

    public class CacheEntity
    {
        public required string Id { get; set; }
        public required byte[]?  Image { get; set; }
    }
}