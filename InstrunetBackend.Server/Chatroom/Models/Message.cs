using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace InstrunetBackend.Server.Chatroom.Models;

[PrimaryKey(nameof(Id))]
[Index(nameof(User))]
public record Message
{
    public long Id { get; set; }
    [MaxLength(30)]
    public required string User { get; set; }
    [MaxLength(250)]
    public required string Content { get; set; }
    public required DateTime Time { get; set; }
    
}
public class PostMessageBody
{
    public required string Content { get; set; }
    public required string Username { get; set; }
    public static implicit  operator Message(PostMessageBody m)
    {
        return new()
        {
            User = m.Username,
            Content = m.Content,
            Time = DateTime.Now
        };
    }
}