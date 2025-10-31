namespace InstrunetBackend.Server.InstrunetModels;

public partial class User
{
    public string Uuid { get; set; } = null!;

    public string Username { get; set; } = null!;

    public string? Email { get; set; }

    public string Password { get; set; } = null!;

    public byte[]? Avatar { get; set; }

    public long Time { get; set; }
}
