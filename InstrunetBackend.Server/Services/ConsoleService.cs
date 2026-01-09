using System.Collections.ObjectModel;
using InstrunetBackend.Server.IndependantModels;
using Timer = System.Timers.Timer;

namespace InstrunetBackend.Server.Services;

public class ConsoleService
{
    private int _dailyCounter = 0;

    public void Add()
    {
        _dailyCounter++; 
    }

    public ConsoleService(ObservableCollection<QueueContext> queue, List<QueueContext> cache)
    {
        Timer t = new Timer();
        t.Interval = 1000;
        t.Elapsed += (sender, args) =>
        {
            if (DateTime.Now.Hour == 0)
            {
                _dailyCounter = 0;
            }
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
                        foreach (var queueContext in queue)
                        {
                            Console.WriteLine(queueContext.Name);
                        }

                        break; 
                    case "cache":
                        Console.WriteLine("Currently in cache: ");
                        foreach (var cacheItem in cache)
                        {
                            Console.WriteLine(cacheItem.Name);
                            
                        }
                        Console.WriteLine($"Count: {cache.Count}. ");
                        break; 
                    case "daily": 
                        Console.WriteLine($"Today: {_dailyCounter}");
                        break; 
                    default:
                        Console.WriteLine("Bad command. ");
                        break; 
                }
            }
        }); 
    }
}