using System;

public class RegisterContextPayload
{
    public required string Username { get; set; }

    public required string Password { get; set; } 

    public string? Email { get; set; }
}
