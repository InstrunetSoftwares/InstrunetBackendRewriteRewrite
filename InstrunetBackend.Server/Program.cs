using InstrunetBackend.Server.Context;
using InstrunetBackend.Server.Endpoints;
using InstrunetBackend.Server.IndependantModels;
using InstrunetBackend.Server.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;

namespace InstrunetBackend.Server;

internal class Program
{
    public static readonly string LibraryCommon = "./lib-runtime/";
    public static string CWebP = LibraryCommon + "cwebp/";
    public static string UM = LibraryCommon + "unlock-music/";
    public static Action<object?, EventArgs>? CleanupHandler
    {
        get
        {
            while (field is null)
            {
                Task.Delay(20).GetAwaiter().GetResult();
            }
            return field;
        }
        set;
    }

    private static (ObservableCollection<QueueContext>, ObservableCollection<SttProcessContext>?, List<MessageModel>)
        Initialize()
    {
        var queue = new ObservableCollection<QueueContext>();
        queue.CollectionChanged += (_, e) =>
        {
            Console.WriteLine("Queue changed. ");
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    var newItem = (QueueContext)e.NewItems![0]!;
                    Task t = new Task( () =>
                        {
                            Console.WriteLine("Task Fired. ");
                            if (!newItem.CancellationToken.IsCancellationRequested)
                            {
                                using var client = new HttpClient();
                                client.Timeout = System.Threading.Timeout.InfiniteTimeSpan; 
                                client.BaseAddress = new Uri("http://andyxie.cn:8201"); 
                                using var formContent = new MultipartFormDataContent(); 
                                formContent.Add(new ByteArrayContent(newItem.File), "stuff", "uploadfile");
                                using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("InstrunetBackend.Server.PROCESS_KEY");
                                using var keyMs = new  MemoryStream();
                                s.CopyToAsync(keyMs);
                                var key =  Encoding.UTF8.GetString(keyMs.ToArray()).Trim(); 
                                Console.WriteLine($"Loaded key: {key}");
                                using var res =  client.PostAsync($"api/process?remoteKey={key}&kind={newItem.Kind}", formContent , newItem.CancellationToken.Token).GetAwaiter().GetResult();
                                if (!res.IsSuccessStatusCode)
                                {
                                    Console.WriteLine($"{res.StatusCode}: {res.ReasonPhrase}");
                                    return; 
                                }

                                var ms = res.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult(); 
                                try
                                {
                                    using var dbContext = new InstrunetDbContext();
                                    dbContext.InstrunetEntries.Add(new()
                                    {
                                        Uuid = newItem.Uuid,
                                        SongName = newItem.Name,
                                        AlbumName = newItem.AlbumName,
                                        LinkTo = newItem.Link ?? "",
                                        Email = newItem.Email,
                                        Albumcover = newItem.AlbumCover,
                                        Artist = newItem.Artist,
                                        Databinary = ms,
                                        Epoch = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds(),
                                        Kind = newItem.Kind,
                                        User = newItem.UserUuid
                                    });
                                    dbContext.SaveChanges();
                                }
                                catch (DbUpdateException dbUpdateException)
                                {
                                    Console.WriteLine(dbUpdateException.Message);
                                }
                            }
                        },
                        newItem.CancellationToken.Token);
                    t.ContinueWith((iT) =>
                    {
                        while (true)
                        {
                            if (iT.IsCompleted || iT.IsCanceled)
                            {
                                queue.RemoveAt(0);
                                break;
                            }
                        }
                    });
                    newItem.ProcessTask = t;
                    if (queue.Count == 1)
                    {
                        queue[0].ProcessTask.Start();
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:

                    if (e.OldItems != null)
                    {
                        foreach (var eOldItem in e.OldItems)
                        {
                            ((QueueContext)eOldItem).Dispose();
                        }
                    }

                    GC.Collect();
                    if (queue.Count >= 1)
                    {
                        queue[0].ProcessTask.Start();
                    }

                    break;
            }
        };
        var messages = File.Exists("./messages.json")
            ? JsonSerializer.Deserialize<List<MessageModel>>(File.ReadAllText("./messages.json"),
                new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<MessageModel>()
            : new List<MessageModel>();
        Console.WriteLine("Decompressing libraries");
        Stream? cWebp = null;
        var netEaseService = new NeteaseMusicService();
        var lrcApiService = new LrcApiService();
        try
        {
            Directory.CreateDirectory(CWebP);
            var executingAssembly = Assembly.GetExecutingAssembly();
            cWebp = executingAssembly
                .GetManifestResourceStream("InstrunetBackend.Server.lib.cwebp.libwebp.zip");
            ZipFile.ExtractToDirectory(cWebp!, CWebP, Encoding.UTF8, true);
            Directory.Delete(CWebP + "__MACOSX", true);
        }
        catch (Exception)
        {
            Console.WriteLine("Unknown error while zipping library. ");
            Environment.Exit(1);
        }
        finally
        {
            cWebp?.Dispose();
            GC.Collect();
        }

        if (OperatingSystem.IsWindows())
        {
            Stream? UMStream = null;
            FileStream? fStream = null;
            try
            {
                Directory.CreateDirectory(UM);
                var executingAssembly = Assembly.GetExecutingAssembly();
                UMStream = executingAssembly.GetManifestResourceStream(
                    "InstrunetBackend.Server.lib.unlock_music.um.exe");
                if (UMStream == null)
                {
                    throw new NullReferenceException("Unlock-music stream is null. ");
                }

                fStream = new FileStream(UM + "um.exe", FileMode.Create);
                UMStream?.CopyTo(fStream);
            }
            catch (NullReferenceException)
            {
                Console.WriteLine("unlock-music library not found. Skipping...");
            }
            catch (Exception)
            {
                Console.WriteLine("Unknown error while extracting unlock-music library. Skipping...");
            }
            finally
            {
                fStream?.Dispose();
                UMStream?.Dispose();
            }
        }
        else
        {
            Console.WriteLine("Music unlock is only available in Windows. Skipping...");
        }

        CleanupHandler = (_, e) =>
        {
            if (e is ConsoleCancelEventArgs args)
            {
                args.Cancel = true;
            }

            Console.WriteLine("Cleaning up...");
            File.WriteAllText("./messages.json", JsonSerializer.Serialize(messages));
            netEaseService.Process?.Kill(true);
            netEaseService.Process?.Dispose();
            Console.WriteLine("NCM Process killed. ");
            lrcApiService.Process?.Kill(true);
            lrcApiService.Process?.Dispose();
            Console.WriteLine("LrcApi Process killed. ");
            Console.WriteLine("Cleanup done. ");
        };

        Console.CancelKeyPress += (_, e) =>
        {
            CleanupHandler(_, e);
        };
        return (queue, null, messages);
    }

    public static void Main(string[] args)
    {
        var res = Initialize();

        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddResponseCompression(o =>
        {
            o.EnableForHttps = true;
            o.Providers.Add<BrotliCompressionProvider>();
            o.Providers.Add<GzipCompressionProvider>();
            o.MimeTypes =
                ResponseCompressionDefaults.MimeTypes.Concat(
                    ["audio/mp3", "image/webp"]);
        });
        builder.Services.Configure<BrotliCompressionProviderOptions>(o =>
        {
            o.Level = CompressionLevel.SmallestSize;
        }); 
        builder.Services.Configure<GzipCompressionProviderOptions>(o =>
        {
            o.Level = CompressionLevel.SmallestSize;
        });
        builder.Services.AddDbContext<InstrunetDbContext>(); 
        // Cors
        builder.Services.AddCors(o =>
        {
            o.AddPolicy("All", p =>
            {
                p.WithOrigins("http://localhost:5173", "https://andyxie.cn:4000", "http://localhost:3000",
                        "https://andyxie.cn:4001", "http://localhost:3001")
                    .WithHeaders("Content-Type").AllowCredentials();
            });
        });
        builder.Services.AddSwaggerGen();
        builder.Services.AddRateLimiter(_ =>
        {
            _.AddPolicy<string>("UploadRateLimiting", context =>
            {
                var ip = context.Request.Host.Host;
                return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new()
                {
                    PermitLimit = 6, AutoReplenishment = true, Window = TimeSpan.FromMinutes(10)
                });
            });
            _.OnRejected = (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                return ValueTask.CompletedTask;
            };
        });

        // Required for session storage. 
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSession(o =>
        {
            o.IdleTimeout = TimeSpan.FromDays(7);
            o.Cookie.HttpOnly = true;
            o.Cookie.IsEssential = true;
            o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        });
        builder.Services.Configure<FormOptions>(o =>
        {
            o.MultipartBodyLengthLimit = 1_000_000_000;
        }); 


        // Payload size
        builder.Services.Configure<KestrelServerOptions>(o => o.Limits.MaxRequestBodySize = 1_000_000_000);

        // Add services to the container.
        builder.Services.AddAuthorization();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();

        app.UseRateLimiter(); 
        
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwagger();
            app.UseSwaggerUI(); 
        }

        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.UseCors("All");

        app.UseAuthorization();
        app.UseSession();

        app.UseResponseCompression(); 
        
        var cache = new List<QueueContext>();
        Timer timer = new Timer((e) =>
        {
            cache.Clear();
            GC.Collect(); 
        }, null, TimeSpan.Zero, TimeSpan.FromDays(2));
        app.MapAllProcessingEndpoints(res.Item1)
            .MapAllGetterEndpoints(cache)
            .MapAllJustTalkEndpoints(res.Item3)
            .MapAllInstrunetCommunityEndpoints()
            .MapAllUserEndpoints()
            .MapAllPlaylistEndpoints()
            .MapAllUnlockMusicEndpoints();


        app.MapGet("/ping", () => Results.Ok("Pong"));
        app.Run();
    }

}