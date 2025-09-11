namespace InstrunetBackend.Server.InstrunetModels;

public partial class InstrunetEntry
{
    public string Uuid { get; set; } = null!;

    public string SongName { get; set; } = null!;

    public string AlbumName { get; set; } = null!;

    public string LinkTo { get; set; } = null!;

    /// <summary>
    /// Binary data of the song. 
    /// </summary>
    public byte[]? Databinary { get; set; }

    /// <summary>
    /// Artist wrote the song.
    /// </summary>
    public string? Artist { get; set; }

    /// <summary>
    /// Kind of the instrumental.
    /// </summary>
    public int? Kind { get; set; }

    /// <summary>
    /// Coverart for the song. 
    /// </summary>
    public byte[]? Albumcover { get; set; }

    public string? Email { get; set; }

    public long Epoch { get; set; }

    /// <summary>
    /// Who&apos;d uploaded
    /// </summary>
    public string? User { get; set; }
}
