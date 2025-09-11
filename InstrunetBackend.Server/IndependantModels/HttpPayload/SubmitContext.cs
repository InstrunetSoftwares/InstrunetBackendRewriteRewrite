namespace InstrunetBackend.Server.IndependantModels.HttpPayload;

public class SubmitContext
{
    public required string Name { get; set;  }
    public string? AlbumName { get; set;  }
    public string? Artist { get; set;  }
    public string? AlbumCover { get; set;  }
    public string? Link { get; set;  }
    public required string File { get; set;  }
    public string? Email { get; set;  }
    public required int[] Kind { get; set;  }
}