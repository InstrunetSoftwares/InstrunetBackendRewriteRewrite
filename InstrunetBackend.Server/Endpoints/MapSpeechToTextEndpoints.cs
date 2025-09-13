using InstrunetBackend.Server.IndependantModels;
using InstrunetBackend.Server.IndependantModels.HttpPayload;
using Microsoft.AspNetCore.Mvc;
using System.Collections.ObjectModel;

namespace InstrunetBackend.Server.Endpoints
{
    public static class MapSpeechToTextEndpoints
    {
        public static RouteGroupBuilder MapUpload(this RouteGroupBuilder app, ObservableCollection<SttProcessContext> queue)
        {
            app.MapPost("/upload", ([FromBody] SttUploadPayload payload) =>
            {
                if (string.IsNullOrWhiteSpace(payload.Email) || !payload.Email.Contains("@"))
                {
                    return Results.BadRequest("Invalid email format. ");
                }
                var uuid = Guid.NewGuid().ToString();

                queue.Add(new()
                {
                    Uuid = uuid,
                    Language = payload.Language,
                    Email = payload.Email,
                    DateTime = DateTimeOffset.Now,
                    File = payload.File.DataUrlToByteArray(),
                    ProcessTask = null!,
                    CancellationToken = new CancellationTokenSource(),
                    CompleteSentence = payload.CompleteSentence
                });
                return Results.Ok();
            });
            return app;
        }
        public static RouteGroupBuilder MapQueue(this RouteGroupBuilder app, ObservableCollection<SttProcessContext> queue)
        {
            app.MapGet("/queuestt", () =>
            {
                return Results.Ok(queue.Select(i => new { i.Uuid, Email = i.Email[..3] + "*****@" + i.Email.Split("@")[1], i.DateTime, i.Language, i.CompleteSentence }));
            });
            return app;
        }
        public static WebApplication MapAllSpeechToTextEndpoints(this WebApplication app, ObservableCollection<SttProcessContext> queue)
        {
            var sttEndpoints = app.MapGroup("api/SttProcessing");
            var methodInfos = typeof(MapSpeechToTextEndpoints).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            foreach (var methodInfo in methodInfos)
            {
                switch(methodInfo.Name)
                {
                    case "MapAllSpeechToTextEndpoints":
                        continue;
                    default:
                        methodInfo.Invoke(null, [sttEndpoints, queue]);
                        continue; 
                }
            }
            return app;
        }
    }
}
