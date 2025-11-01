namespace InstrunetBackend.Server.IndependantModels.HttpPayload;

public class NcmUrlContext
{
    public required long Id { get; set;  }
    public required int[] Kind { get; set; }
    public string? Email { get; set; }
}