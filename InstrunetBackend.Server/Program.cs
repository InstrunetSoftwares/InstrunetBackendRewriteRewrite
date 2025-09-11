using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using InstrunetBackend.Server.Context;
using InstrunetBackend.Server.Endpoints;
using InstrunetBackend.Server.IndependantModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.International.Converters.TraditionalChineseToSimplifiedConverter;

namespace InstrunetBackend.Server;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

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

        app.UseHttpsRedirection();

        app.UseAuthorization();

        var queue = new ObservableCollection<QueueContext>();
        queue.CollectionChanged += (s, e) =>
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    var newItem = (QueueContext)e.NewItems![0]!;
                    newItem.ProcessTask = new Task(() =>
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
                                    System.IO.File.OpenWrite($"./tmp/instrunet/pre-process/{newItem.Uuid}");
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
                                if (p.ExitCode == 0)
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

                                var processedFileStream = File.OpenRead("./tmp/instrunet/post-process/{fileName}");
                                using var ms = new MemoryStream();
                                processedFileStream.CopyTo(ms);
                                processedFileStream.Dispose();

                                try
                                {
                                    using var dbContext = new InstrunetDbContext();
                                    dbContext.InstrunetEntries.Add(new()
                                    {
                                        Uuid = newItem.Uuid, SongName = newItem.Name, AlbumName = newItem.AlbumName,
                                        LinkTo = newItem.Link ?? "", Email = newItem.Email, //Far more
                                    });
                                    dbContext.SaveChanges();
                                }
                                catch (DbUpdateException e)
                                {
                                    Console.WriteLine(e.Message);
                                }
                            }
                        },
                        newItem.CancellationToken.Token);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    queue[0].ProcessTask.Start();
                    break;
            }
        };

        app.MapAllProcessingEndpoints(queue);

        app.Run();
    }
}