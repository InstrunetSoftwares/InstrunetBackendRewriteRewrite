namespace InstrunetBackend.Server;

public class SongImageCache
{
    public ICollection<(string, byte[]?)> ImageCacheCollection { get; set; } =  new List<(string, byte[]?)>();
}