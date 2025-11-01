namespace InstrunetBackend.Server.IndependantModels.HttpPayload;

public class MessagePayload
{
    public required string Username { get; set;  }
    public string? Text { get; set;  }
    public byte[]? Image { get; set;  }
    
}