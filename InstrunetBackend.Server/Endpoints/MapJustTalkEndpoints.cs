using System.Reflection;
using InstrunetBackend.Server.IndependantModels;
using InstrunetBackend.Server.IndependantModels.HttpPayload;
using Microsoft.AspNetCore.Mvc;

namespace InstrunetBackend.Server.Endpoints;

public static class MapJustTalkEndpoints
{
    public static RouteGroupBuilder LongPolling(this RouteGroupBuilder app, List<object> messages)
    {
        app.MapGet("/JustTalkLongPolling", (long lastMessageTime, HttpContext httpContext) =>
        {
            DateTimeOffset lastMessageDate = DateTimeOffset.FromUnixTimeMilliseconds(lastMessageTime);
            while (true)
            {
                httpContext.RequestAborted.ThrowIfCancellationRequested();
                if (!messages.Any())
                {
                    continue;
                }

                var afterTime = messages.Where(i =>
                        (i as MessageModel)!.SentTime.ToUnixTimeMilliseconds() >
                        lastMessageDate.ToUnixTimeMilliseconds())
                    .OrderBy(i => (i as MessageModel)!.SentTime).ToList();
                if (!afterTime.Any())
                {
                    Task.Delay(100).GetAwaiter().GetResult();
                    continue;
                }

                return Results.Json(afterTime);
            }
        });
        return app;
    }

    public static RouteGroupBuilder GetAll(this RouteGroupBuilder app, List<object> messages)
    {
        app.MapGet("/JustTalkGetAll", () => Results.Json(messages));
        return app;
    }

    public static RouteGroupBuilder Submit(this RouteGroupBuilder app, List<object> messages)
    {
        app.MapPost("/submit", ([FromBody] MessagePayload messagePayload) =>
        {
            if (messagePayload.Image is null && messagePayload.Text is null)
            {
                return Results.BadRequest("ARG_INVALID");
            }

            if (messagePayload.Image is null && messagePayload.Text is not null)
            {
                messages.Add(new MessageModel
                {
                    Modifiers = [], SentTime = DateTimeOffset.Now, Text = messagePayload.Text,
                    Username = messagePayload.Username, Image = null,
                });
                return Results.Ok();
            }

            if (messagePayload.Image is not null && messagePayload.Text is null)
            {
                messages.Add(new MessageModel
                {
                    Modifiers = [], SentTime = DateTimeOffset.Now, Image = messagePayload.Image,
                    Username = messagePayload.Username, Text = null
                });
                return Results.Ok();
            }

            return Results.BadRequest("ARG_INVALID");
        });
        return app; 
    }

    public static WebApplication MapAllJustTalkEndpoints(this WebApplication app, List<object> messages)
    {
        var justTalkApis = app.MapGroup("/api/v1/justTalkController");
        var methods = typeof(MapJustTalkEndpoints).GetMethods(BindingFlags.Static | BindingFlags.Public);
        foreach (var methodInfo in methods)
        {
            switch (methodInfo.Name)
            {
                case "MapAllJustTalkEndpoints":
                    continue;
                default:
                    methodInfo.Invoke(null, [justTalkApis, messages]);
                    continue;
            }
        }

        return app;
    }
}