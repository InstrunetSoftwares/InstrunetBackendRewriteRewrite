using System.Diagnostics;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("InstrunetRemoteProcessor.PROCESS_KEY");
var key = manifestResourceStream.EncodeToString(); 
manifestResourceStream?.Dispose();
Console.WriteLine($"Loaded key: {key}");
app.UseRouting();
app.UseWebSockets(); 
app.MapPost("/api/process", async (HttpContext context,string remoteKey,  [FromForm] IFormFile stuff, int kind) =>
{
    if (remoteKey != key)
    {
        return Results.Unauthorized();
    }
    string uuid = Guid.NewGuid().ToString();
    using var newItemMs = new MemoryStream(); 
    stuff.CopyTo(newItemMs);
    var newItem = newItemMs.ToArray();
    if (newItem.Length == 0)
    {
        return Results.BadRequest();
    }

    var modelName = "";
    switch (kind)
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
        File.OpenWrite($"./tmp/instrunet/pre-process/{uuid}");
    fileStream.Write(newItem, 0, newItem.Length);
    fileStream.Dispose();

    var p = new Process();
    p.StartInfo.FileName = "audio-separator";
    p.StartInfo.WorkingDirectory =
        Directory.GetCurrentDirectory();
    p.StartInfo.Arguments = kind == 6
        ? $"./tmp/instrunet/pre-process/{uuid} -m {modelName} --output_format mp3 --output_dir ./tmp/instrunet/post-process --single_stem guitar"
        : kind == 4
            ? $"./tmp/instrunet/pre-process/{uuid} -m {modelName} --output_format mp3 --output_dir ./tmp/instrunet/post-process --single_stem drums"
            : $"./tmp/instrunet/pre-process/{uuid} --model_filename {modelName} --mdx_enable_denoise  --mdx_segment_size 4000 --mdx_overlap 0.85 --mdx_batch_size 300  --output_format mp3 --output_dir ./tmp/instrunet/post-process";
    p.StartInfo.EnvironmentVariables["http-proxy"] = "http://127.0.0.1:7890";
    p.StartInfo.EnvironmentVariables["https-proxy"] = "http://127.0.0.1:7890";
    p.Start();
    await p.WaitForExitAsync(); 
    File.Delete($"./tmp/instrunet/pre-process/{uuid}");
    if (p.ExitCode != 0)
    {
        p.Dispose();
        return Results.InternalServerError();
    }

    p.Dispose();
    var fileName = "";
    switch (kind)
    {
        case 0:
            fileName = $"{uuid}_(Instrumental)_UVR-MDX-NET-Inst_HQ_4.mp3";
            break;
        case 1:
            fileName = $"{uuid}_(Instrumental)_UVR_MDXNET_KARA_2.mp3";
            break;
        case 2:
            fileName = $"{uuid}_(Vocals)_UVR_MDXNET_KARA.mp3";
            break;
        case 3:
            fileName = $"{uuid}_(Bass)_kuielab_a_bass.mp3";
            break;
        case 4:
            fileName = $"{uuid}_(Drums)_htdemucs_ft.mp3";
            break;
        case 5:

            fileName = $"{uuid}_(Vocals)_UVR_MDXNET_Main.mp3";
            break;
        case 6:
            fileName = $"{uuid}_(Guitar)_htdemucs_6s.mp3";
            break;
    }

    var processedFileStream = File.OpenRead($"./tmp/instrunet/post-process/{fileName}");
    var ms = new MemoryStream();
    processedFileStream.CopyTo(ms);
    processedFileStream.Dispose();
    return Results.File(ms.ToArray());
}).DisableAntiforgery();
app.MapGet("/api/ping", () => Results.Ok("Pong")); 
app.Run();

static class Ext
{
    extension(Stream? stream)
    {
        public string EncodeToString()
        {
            using var mStream = new MemoryStream(); 
            stream?.CopyTo(mStream);
            return Encoding.UTF8.GetString(mStream.ToArray());
        }
    }
}


