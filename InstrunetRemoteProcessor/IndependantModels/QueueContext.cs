using System.Diagnostics;
using InstrunetBackend.Server.InstrunetModels;

namespace InstrunetBackend.Server.IndependantModels;

public class QueueContext : IDisposable
{
    public required CancellationTokenSource CancellationToken { get; set; }
    public required string Uuid { get; set; }
    public required string Name { get; set; }
    public required string AlbumName { get; set; }
    public required string Artist { get; set; }
    public required int Kind { get; set; }
    public required byte[] File { get; set; }
    public string? Link { get; set; }
    public string? UserUuid { get; set; }
    public byte[]? AlbumCover { get; set; }
    public required Task ProcessTask { get; set; }
    public string? Email { get; set; }
    public required DateTime DateTimeUploaded { get; set; }

    public void Dispose()
    {
        while (true)
        {
            if (ProcessTask.IsCompleted || ProcessTask.IsCanceled)
            {
                CancellationToken.Dispose();
                ProcessTask.Dispose();
                Console.WriteLine($"{Name} disposed successfully. ");
                break;
            }
        }
    }

    public static implicit operator QueueContext(InstrunetEntry entry)
    {
        return new QueueContext
        {
            CancellationToken = null!,
            Uuid = entry.Uuid,
            Name = entry.SongName,
            AlbumName = entry.AlbumName,
            Artist = entry.Artist!,
            Kind = entry.Kind!.Value,
            File = entry.Databinary!,
            ProcessTask = null!,
            DateTimeUploaded =
                DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString().Length == entry.Epoch.ToString().Length
                    ? DateTimeOffset.FromUnixTimeMilliseconds(entry.Epoch).LocalDateTime
                    : DateTimeOffset.FromUnixTimeSeconds(entry.Epoch).LocalDateTime,
            AlbumCover = entry.Albumcover,
            Email = entry.Email,
            Link = entry.LinkTo,
            UserUuid = entry.User
        };
    }
}