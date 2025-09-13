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
using static InstrunetBackend.Server.IndependantModels.HttpPayload.SttUploadPayload;

namespace InstrunetBackend.Server;

internal class Program
{
    public static readonly string LibraryCommon = "./lib-runtime/";
    public static string CWebP = LibraryCommon + "cwebp/";
    public static string UM = LibraryCommon + "unlock-music/";

    private static void Initialize()
    {
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
                UMStream = executingAssembly.GetManifestResourceStream("InstrunetBackend.Server.lib.unlock_music.um.exe");
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

        void exitHandler(dynamic? _, object e)
        {
            if (e is ConsoleCancelEventArgs)
            {
                ((ConsoleCancelEventArgs)e).Cancel = true;
            }
            Console.WriteLine("Cleaning up...");
            netEaseService.Process?.Kill();
            netEaseService.Process?.Dispose();
            lrcApiService.Process?.Kill();
            lrcApiService.Process?.Dispose();
        }

        Console.CancelKeyPress += exitHandler;
    }

    public static void Main(string[] args)
    {
        Initialize();

        var builder = WebApplication.CreateBuilder(args);

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

        // Required for session storage. 
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSession(o =>
        {
            o.IdleTimeout = TimeSpan.FromDays(7);
            o.Cookie.HttpOnly = true;
            o.Cookie.IsEssential = true;
            o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        });

        // Payload size
        builder.Services.Configure<KestrelServerOptions>(o => o.Limits.MaxRequestBodySize = 1_000_000_000);

        // Add services to the container.
        builder.Services.AddAuthorization();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.UseCors("All");

        app.UseAuthorization();

        app.UseSession();

        var queue = new ObservableCollection<QueueContext>();
        queue.CollectionChanged += (_, e) =>
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    var newItem = (QueueContext)e.NewItems![0]!;
                    Task t = new Task(() =>
                        {
                            if (!newItem.CancellationToken.IsCancellationRequested)
                            {
                                if (newItem.File.Length == 0)
                                {
                                    return;
                                }

                                var modelName = "";
                                switch (newItem.Kind)
                                {
                                    case 0:
                                        modelName = "UVR-MDX-NET-Inst_HQ_4.onnx";
                                        break;
                                    case 1:
                                        modelName = "UVR_MDXNET_KARA_2.onnx";
                                        break;
                                    case 2:
                                        modelName = "UVR_MDXNET_KARA.onnx";
                                        break;
                                    case 3:
                                        modelName = "kuielab_a_bass.onnx";
                                        break;
                                    case 4:
                                        // Drums
                                        modelName = "htdemucs_ft.yaml";
                                        break;
                                    case 5:
                                        modelName = "UVR_MDXNET_Main.onnx";
                                        break;
                                    case 6:
                                        modelName = "htdemucs_6s.yaml";
                                        break;
                                }

                                // Write file to disk. 
                                Directory.CreateDirectory("./tmp/instrunet/pre-process");
                                var fileStream =
                                    File.OpenWrite($"./tmp/instrunet/pre-process/{newItem.Uuid}");
                                fileStream.Write(newItem.File, 0, newItem.File.Length);
                                fileStream.Dispose();

                                var p = new Process();
                                p.StartInfo.FileName = "audio-separator";
                                p.StartInfo.WorkingDirectory =
                                    Directory.GetCurrentDirectory();
                                p.StartInfo.Arguments = newItem.Kind == 6
                                    ? $"./tmp/instrunet/pre-process/{newItem.Uuid} -m {modelName} --output_format mp3 --output_dir ./tmp/instrunet/post-process --single_stem guitar"
                                    : newItem.Kind == 4
                                        ? $"./tmp/instrunet/pre-process/{newItem.Uuid} -m {modelName} --output_format mp3 --output_dir ./tmp/instrunet/post-process --single_stem drums"
                                        : $"./tmp/instrunet/pre-process/{newItem.Uuid} --model_filename {modelName} --mdx_enable_denoise  --mdx_segment_size 4000 --mdx_overlap 0.85 --mdx_batch_size 300  --output_format mp3 --output_dir ./tmp/instrunet/post-process";
                                p.StartInfo.EnvironmentVariables["http-proxy"] = "http://127.0.0.1:7890";
                                p.StartInfo.EnvironmentVariables["https-proxy"] = "http://127.0.0.1:7890";
                                p.Start();
                                while (!p.HasExited)
                                {
                                    if (newItem.CancellationToken.IsCancellationRequested)
                                    {
                                        p.Kill();
                                        p.Dispose();
                                        Console.WriteLine("Cancelled");
                                        return;
                                    }

                                    Task.Delay(500).GetAwaiter().GetResult();
                                }

                                File.Delete($"./tmp/instrunet/pre-process/{newItem.Uuid}");
                                if (p.ExitCode != 0)
                                {
                                    p.Dispose();
                                    return;
                                }

                                p.Dispose();
                                var fileName = "";
                                switch (newItem.Kind)
                                {
                                    case 0:
                                        fileName = $"{newItem.Uuid}_(Instrumental)_UVR-MDX-NET-Inst_HQ_4.mp3";
                                        break;
                                    case 1:
                                        fileName = $"{newItem.Uuid}_(Instrumental)_UVR_MDXNET_KARA_2.mp3";
                                        break;
                                    case 2:
                                        fileName = $"{newItem.Uuid}_(Vocals)_UVR_MDXNET_KARA.mp3";
                                        break;
                                    case 3:
                                        fileName = $"{newItem.Uuid}_(Bass)_kuielab_a_bass.mp3";
                                        break;
                                    case 4:
                                        fileName = $"{newItem.Uuid}_(Drums)_htdemucs_ft.mp3";
                                        break;
                                    case 5:

                                        fileName = $"{newItem.Uuid}_(Vocals)_UVR_MDXNET_Main.mp3";
                                        break;
                                    case 6:
                                        fileName = $"{newItem.Uuid}_(Guitar)_htdemucs_6s.mp3";
                                        break;
                                }

                                var processedFileStream = File.OpenRead($"./tmp/instrunet/post-process/{fileName}");
                                var ms = new MemoryStream();
                                processedFileStream.CopyTo(ms);
                                processedFileStream.Dispose();

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
                                        Databinary = ms.ToArray(),
                                        Epoch = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds(),
                                        Kind = newItem.Kind,
                                        User = newItem.UserUuid
                                    });
                                    dbContext.SaveChanges();
                                    ms.Dispose();
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

        // Datas
        var cache = new List<QueueContext>();
        var messages = new List<object>();
        var sttQueue = new ObservableCollection<SttProcessContext>();
        sttQueue.CollectionChanged += (_, e) =>
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    var newItem = (SttProcessContext)e.NewItems![0]!;
                    Task t = new Task(() =>
                    {
                        var inputDir = "./tmp/stt/input";
                        var outputDir = "./tmp/stt/output";
                        Directory.CreateDirectory(inputDir);
                        Directory.CreateDirectory(outputDir);

                        File.WriteAllBytes(inputDir + $"/{newItem.Uuid}", newItem.File);

                        var arg = $"args=../{newItem.Uuid} --model turbo " + new Func<string>(() =>
                        newItem.Language == LanguageType.Automatic
                            ? ""
                            : $"--language {newItem.Language.ToString()}")() + (newItem.CompleteSentence
                        ? " --initial_prompt \"Keep sentences' integrity. Don't cut the sentence off when it's not finished.\""
                        : "");
                        var p = new Process();
                        p.StartInfo.FileName = OperatingSystem.IsWindows()
                                ? "C:\\ProgramData\\miniconda3\\envs\\whisper\\Scripts\\whisper.exe"
                                : "/Users/dt/anaconda3/envs/whisper/bin/whisper";
                        p.StartInfo.WorkingDirectory = outputDir;
                        p.StartInfo.Arguments = arg;
                        p.Start();
                        while (!p.HasExited)
                        {
                            if (newItem.CancellationToken.IsCancellationRequested)
                            {
                                p.Kill();
                                p.Dispose();
                                Console.WriteLine($"Cancelled stt: {newItem.Email}");
                                Directory.Delete(outputDir, true);
                                return;
                            }
                            Task.Delay(500).GetAwaiter().GetResult();
                        }
                        p.Dispose();
                        TarFile.CreateFromDirectory(outputDir, $"{outputDir}/{newItem.Uuid}.tar", false);
                        using var smtp = new SmtpClient("smtp.qq.com", 587);
                        smtp.EnableSsl = true;
                        smtp.Credentials = new NetworkCredential("3095864740@qq.com", "ttbluipankwrddfd");
                        smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                        var mailMessage = new MailMessage
                        {
                            From = new MailAddress("xiey0@qq.com"),
                            Subject = "转文本完成",
                            Attachments =
                            {
                                new(new MemoryStream(System.IO.File.ReadAllBytes($"{outputDir}/{newItem.Uuid}.tar")), "结果.tar",
                                    "application/x-zip-compressed"),
                            },
                            Body = "见附件",
                            IsBodyHtml = true
                        };
                        mailMessage.To.Add(new MailAddress(newItem.Email));
                        try
                        {
                            smtp.SendMailAsync(mailMessage).WaitAsync(TimeSpan.FromSeconds(20)).ContinueWith((t) =>
                            {
                                var cap = newItem.Email;
                                Console.WriteLine("Send email timeout! Please check email configuration. " + cap);
                            }).GetAwaiter().GetResult();
                        }
                        catch (SmtpException smtpException)
                        {
                            Console.WriteLine(smtpException.Message);
                        }
                        finally
                        {
                            Directory.Delete(inputDir, true);
                            Directory.Delete(outputDir, true);
                        }




                    });
                    t.ContinueWith((iT) =>
                    {
                        while (true)
                        {
                            if (iT.IsCanceled || iT.IsCompleted)
                            {
                                sttQueue.RemoveAt(0);
                                break;
                            }
                        }
                    });
                    newItem.ProcessTask = t;
                    if (sttQueue.Count == 1)
                    {
                        sttQueue[0].ProcessTask.Start();
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                    {
                        foreach (var eOldItem in e.OldItems)
                        {
                            ((SttProcessContext)eOldItem).Dispose();
                        }
                    }
                    GC.Collect();
                    if (sttQueue.Count >= 1)
                    {
                        sttQueue[0].ProcessTask.Start();
                    }
                    break;
            }
        };

        app.MapAllProcessingEndpoints(queue)
            .MapAllGetterEndpoints(cache)
            .MapAllJustTalkEndpoints(messages)
            .MapAllInstrunetCommunityEndpoints()
            .MapAllUserEndpoints()
            .MapAllPlaylistEndpoints()
            .MapAllSpeechToTextEndpoints(sttQueue).MapAllUnlockMusicEndpoints();


        app.MapGet("/ping", () => Results.Ok("Pong"));

        Console.WriteLine(Environment.OSVersion.Platform);
        app.Run();
    }
}