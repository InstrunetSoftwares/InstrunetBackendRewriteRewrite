using System.Diagnostics;

namespace InstrunetBackend.Server.IndependantModels;

public class QueueContext
{
    public required CancellationTokenSource CancellationToken { get; set; }
    public required string Uuid { get; set;  }
    public required string Name { get; set;  }
    public required string AlbumName { get; set;  }
    public required string Artist { get; set; }
    public required int Kind { get; set; }
    public required byte[] File { get; set;  }
    public string? Link { get; set;  }
    public string? UserUuid { get; set;  }
    public byte[]? AlbumCover { get; set;  }
    public required Task ProcessTask { get; set;  }
    public string? Email { get; set;  }
    public required DateTime DateTimeUploaded { get; set;  }
}