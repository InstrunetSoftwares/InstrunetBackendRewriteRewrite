namespace InstrunetBackend.Server.IndependantModels;

public class PostCommentPayload
{
    public required string Content { get; set; }
    public required string Master { get; set; }
}