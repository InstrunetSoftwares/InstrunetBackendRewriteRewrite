using InstrunetBackend.Server.Chatroom.Models;
using Microsoft.EntityFrameworkCore;

namespace InstrunetBackend.Server.Chatroom;

public class ChatroomDbContext : DbContext
{
    public  virtual DbSet<Message> Messages { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=chatroom.db");
    }
    
}