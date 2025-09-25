namespace InstrunetBackend.Server.IndependantModels.HttpPayload;

public class SubmitContext<IFormFile>
{
    public required string name { get; set;  }
    public string? albumName { get; set;  }
    public string? artist { get; set;  }
    public IFormFile? albumCover { get; set;  }
    public string? link { get; set;  }
    public required IFormFile fileBinary { get; set;  }
    public string? email { get; set;  }
    public required int[] kind { get; set;  }
}

public class SubmitContext
{
    public required string name { get; set; }
    public string? albumName { get; set; }
    public string? artist { get; set; }
    public string? albumCover { get; set; }
    public string? link { get; set; }
    public required byte[] fileBinary { get; set; }
    public string? email { get; set; }
    public required int[] kind { get; set; }
}

