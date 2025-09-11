namespace InstrunetBackend.Server.InstrunetModels;

public partial class Playlist
{
    public string Owner { get; set; } = null!;

    public string Uuid { get; set; } = null!;

    public string Content { get; set; } = null!;

    public bool Private { get; set; }

    public byte[]? Tmb { get; set; }

    public string? Title { get; set; }
}
