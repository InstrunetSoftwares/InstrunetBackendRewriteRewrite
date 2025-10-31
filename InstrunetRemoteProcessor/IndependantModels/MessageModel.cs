namespace InstrunetBackend.Server.IndependantModels;

public class MessageModel
{
    public required byte[]? Image { get; set;  }
    public required string? Text { get; set;  }


    public required DateTimeOffset SentTime { get; set;  }
    public required string Username { get; set;  }
    public required MessageModifiers[] Modifiers { get; set;  }
}
public enum MessageModifiers
{
    
}