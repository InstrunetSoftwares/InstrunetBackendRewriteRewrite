using InstrunetBackend.Server.IndependantModels.HttpPayload;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Reflection;

namespace InstrunetBackend.Server.Endpoints
{
    public static class MapUnlockMusicEndpoints
    {
        public static RouteGroupBuilder MapSubmit(this RouteGroupBuilder app)
        {
            app.MapPost("/decrypterSubmit", ([FromBody] UnlockMusicSubmitPayload decrypterSubmitPayload) =>
            {

                var inputDir = "./tmp/unlock-music/input";
                var outputDir = "./tmp/unlock-music/output";
                Directory.CreateDirectory(inputDir);
                string uuid = Guid.NewGuid().ToString();
                try
                {
                    File.WriteAllBytes($"{inputDir}/{uuid}_{decrypterSubmitPayload.FileName}", decrypterSubmitPayload.FileInDataUri.DataUrlToByteArray());
                    using var p = new Process
                    {
                        StartInfo = new()
                        {
                            FileName = $"{Program.UM}um.exe",
                            Arguments = $"-o {outputDir} -i \"{inputDir}/{uuid}_{decrypterSubmitPayload.FileName}\""
                        }
                    };
                    p.Start();
                    p.WaitForExit();
                    if (p.ExitCode != 0)
                    {
                        return Results.BadRequest(new { message = "Decryption failed." });
                    }
                    var fileName = Directory.EnumerateFiles(outputDir).ToList()[0];
                    var f = File.ReadAllBytes(fileName);

                    return Results.Json(new
                    {
                        data = "data:application/octet-stream;base64," + Convert.ToBase64String(f),
                        fileName
                    });
                }
                finally
                {
                    Directory.Delete(outputDir, true);
                    File.Delete($"{inputDir}/{uuid}_{decrypterSubmitPayload.FileName}");
                    GC.Collect(); 
                }

            });
            return app;
        }
        public static RouteGroupBuilder MapGetQqm(this RouteGroupBuilder app)
        {
            app.MapGet("/qqm", () =>
            {
                var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("InstrunetBackend.Server.lib.QQMBinary.QQMusicSetup18.44.exe");
                if (stream is null)
                {
                    return Results.InternalServerError();
                }
                var memstream = new MemoryStream();
                stream.CopyTo(memstream);
                var arr = memstream.ToArray();
                stream.Dispose();
                memstream.Dispose();
                return Results.File(arr, "application/vnd.microsoft.portable-executable", "QQMusicSetup18.44.exe", true);
            });
            return app;
        }
        public static WebApplication MapAllUnlockMusicEndpoints(this WebApplication app)
        {
            var unlockMusicEndpoints = app.MapGroup("api/Decrypter");
            unlockMusicEndpoints.WithTags("Decrypter");
            var methodInfos = typeof(MapUnlockMusicEndpoints).GetMethods(BindingFlags.Static | BindingFlags.Public);
            foreach (var item in methodInfos)
            {
                switch (item.Name)
                {
                    case "MapAllUnlockMusicEndpoints":
                        continue;
                    default:
                        item.Invoke(null, [unlockMusicEndpoints]);
                        continue;

                }
            }
            return app;
        }
    }
}
