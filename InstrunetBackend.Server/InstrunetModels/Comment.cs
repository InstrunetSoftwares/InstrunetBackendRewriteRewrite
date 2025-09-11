namespace InstrunetBackend.Server.InstrunetModels;

public partial class Comment
{
    public string Uuid { get; set; } = null!;

    public string Content { get; set; } = null!;

    public ulong Date { get; set; }

    public string Poster { get; set; } = null!;

    public string Master { get; set; } = null!;
}
