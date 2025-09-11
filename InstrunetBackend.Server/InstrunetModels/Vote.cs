namespace InstrunetBackend.Server.InstrunetModels;

public partial class Vote
{
    public string User { get; set; } = null!;

    public string Master { get; set; } = null!;

    public string Uuid { get; set; } = null!;

    public bool IsUpvote { get; set; }
}
