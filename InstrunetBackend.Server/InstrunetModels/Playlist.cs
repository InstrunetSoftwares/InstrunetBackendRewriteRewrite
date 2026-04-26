namespace InstrunetBackend.Server.InstrunetModels;

public class Playlist
{
    public string Owner { get; set; } = null!;

    public string Uuid { get; set; } = null!;

    public string Content { get; set; } = null!;

    public bool Private { get; set; }

    public byte[]? Tmb { get; set; }

    public string? Title { get; set; }
    public DateTime? Created { get; set; }
    public DateTime? Modified { get; set; }
}