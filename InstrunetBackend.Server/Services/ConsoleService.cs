using System.Collections.ObjectModel;
using InstrunetBackend.Server.Context;
using InstrunetBackend.Server.IndependantModels;
using InstrunetBackend.Server.lib;
using Timer = System.Timers.Timer;

namespace InstrunetBackend.Server.Services;

public class ConsoleService
{
    private int _dailyCounter;

    public ConsoleService(ObservableCollection<QueueContext> queue, WebApplication app)
    {
        var t = new Timer();
        t.Interval = 1000;
        t.Elapsed += (sender, args) =>
        {
            if (DateTime.Now.Hour == 0) _dailyCounter = 0;
        };
        Console.WriteLine(DateTime.Now.Hour);
        Task.Run(() =>
        {
            while (true)
            {
                var command = Console.ReadLine();
                switch (command)
                {
                    case "queue":
                        Console.WriteLine("Currently running: ");
                        foreach (var queueContext in queue) Console.WriteLine(queueContext.Name);

                        break;
                    case "cache":
                        if (app.Services.GetService<List<QueueContext>>() is { } entryCache)
                        {
                            Console.WriteLine("Currently in cache: ");
                            foreach (var cacheItem in entryCache) 
                                Console.WriteLine(cacheItem.Name);
                            Console.WriteLine($"Count: {entryCache.Count}. ");
                        }
                        else
                        {
                            Console.WriteLine("EntryCache is disabled right now. ");
                        }
                        
                        if (app.Services.GetService<List<CacheEntity>>() is {} imageCache)
                        {
                            foreach (var item in imageCache) 
                                Console.WriteLine(item.Id);
                            Console.WriteLine($"Image cache count: {imageCache.Count}. ");
                        }
                        else
                        {
                            Console.WriteLine("ImageCache is disabled right now. ");
                        }

                        break;
                    case "daily":
                        Console.WriteLine($"Today: {_dailyCounter}");
                        break;
                    case "optimize":
                        var w = LibraryHelper.CreateWebPEncoderBuilder();
                        if (w is null)
                        {
                            "Failed to run optimize: unable to access webp. ".PrintLn();
                            break;
                        }

                        var encoder = w.Resize(500, 0).MultiThread()
                            .CompressionConfig(x => x.Lossy(y => y.Quality(40).Size(75000))).Build();

                        using (var db = new InstrunetDbContext())
                        {
                            var s = db.InstrunetEntries
                                .Where(i => i.Albumcover != null && i.Albumcover.Length > 75 * 1000)
                                .Select(i => new
                                {
                                    i.Uuid, i.SongName, i.AlbumName, i.Artist, i.Albumcover, i.Kind
                                });
                            foreach (var x1 in s)
                            {
                                x1.Uuid.PrintLn();
                                x1.SongName.PrintLn();
                                x1.Artist?.PrintLn();
                                using var @in = new MemoryStream(x1.Albumcover!);
                                using var @out = new MemoryStream();
                                try
                                {
                                    encoder.Encode(@in, @out);
                                }
                                catch (Exception ex)
                                {
                                    break;
                                }

                                using var newDbWrite = new InstrunetDbContext();
                                newDbWrite.InstrunetEntries.First(i => i.Uuid == x1.Uuid).Albumcover = @out.ToArray();
                                newDbWrite.SaveChanges();
                            }
                        }

                        break;

                    default:
                        Console.WriteLine("Bad command. ");
                        break;
                }
            }
        });
    }

    public void Add()
    {
        _dailyCounter++;
    }
}