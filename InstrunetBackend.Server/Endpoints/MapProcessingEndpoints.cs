using System.Collections.ObjectModel;
using System.Reflection;
using InstrunetBackend.Server.Context;
using InstrunetBackend.Server.IndependantModels;
using InstrunetBackend.Server.IndependantModels.HttpPayload;
using InstrunetBackend.Server.InstrunetModels;
using Microsoft.International.Converters.TraditionalChineseToSimplifiedConverter;

namespace InstrunetBackend.Server.Endpoints;

internal static class MapProcessingEndpoints
{
    public static WebApplication Queue(this WebApplication app, ObservableCollection<QueueContext> queue)
    {
        app.MapGet("/queue", () =>
        {
            return queue.Select(i => new
            {
                i.Name, i.Artist, i.AlbumName, i.Kind, i.DateTimeUploaded
            });
        });
        return app;
    }

    public static WebApplication Submit(this WebApplication app, ObservableCollection<QueueContext> queue)
    {
        app.MapPost("/submit", new Func<SubmitContext, HttpContext, IResult>((submitContext, httpContext) =>
        {
            if (submitContext.AlbumName is null || string.IsNullOrEmpty(submitContext.AlbumName.Trim()))
            {
                submitContext.AlbumName = "未知专辑";
            }

            if (submitContext.Artist is null || string.IsNullOrEmpty(submitContext.Artist.Trim()))
            {
                submitContext.Artist = "未知艺术家";
            }

            if (string.IsNullOrEmpty(submitContext.Name.Trim()))
            {
                return Results.BadRequest(new
                {
                    Message = "文件不存在"
                });
            }

            foreach (var kind in submitContext.Kind)
            {
                using var context = new InstrunetDbContext();
                var rep = context.InstrunetEntries.Count(i => ((i.SongName == (submitContext.Name) &&
                                                                i.Artist! == (submitContext.Artist)) ||
                                                               (i.SongName == (
                                                                    ChineseConverter.Convert(submitContext
                                                                            .Name,
                                                                        ChineseConversionDirection
                                                                            .SimplifiedToTraditional)) &&
                                                                i.Artist! == (
                                                                    ChineseConverter.Convert(submitContext
                                                                            .Artist,
                                                                        ChineseConversionDirection
                                                                            .SimplifiedToTraditional))) ||
                                                               (i.SongName == (
                                                                    ChineseConverter.Convert(submitContext
                                                                            .Name,
                                                                        ChineseConversionDirection
                                                                            .TraditionalToSimplified)) &&
                                                                i.Artist! == (
                                                                    ChineseConverter.Convert(submitContext
                                                                            .Artist,
                                                                        ChineseConversionDirection
                                                                            .TraditionalToSimplified))
                                                               )) && i.Kind == kind);


                var rep2 = queue.Count(i => ((i.Name == (submitContext.Name) &&
                                              i.Artist! == (submitContext.Artist)) ||
                                             (i.Name == (
                                                  ChineseConverter.Convert(submitContext
                                                          .Name,
                                                      ChineseConversionDirection
                                                          .SimplifiedToTraditional)) &&
                                              i.Artist! == (
                                                  ChineseConverter.Convert(submitContext
                                                      .Artist, ChineseConversionDirection.SimplifiedToTraditional))) ||
                                             (i.Name == (
                                                  ChineseConverter.Convert(submitContext
                                                      .Name, ChineseConversionDirection.TraditionalToSimplified)) &&
                                              i.Artist! == (
                                                  ChineseConverter.Convert(submitContext
                                                      .Artist, ChineseConversionDirection.TraditionalToSimplified))
                                             )) && i.Kind == kind);
                if (rep == 0 && rep2 == 0)
                {
                    var newCancellationTokenSource = new CancellationTokenSource();
                    try
                    {
                        queue.Add(new()
                        {
                            Uuid = Guid.NewGuid()
                                .ToString(),
                            Name = submitContext.Name,
                            Artist = submitContext.Artist,
                            AlbumName = submitContext.AlbumName,
                            Kind = kind,
                            Email = submitContext.Email,
                            Link = submitContext.Link,
                            UserUuid = httpContext.Session.GetString("uuid") ?? null,
                            AlbumCover = submitContext.AlbumCover?.DataUrlToByteArray(),
                            DateTimeUploaded = DateTime.Now,
                            CancellationToken = new CancellationTokenSource(),
                            File = submitContext.File.DataUrlToByteArray(),
                            ProcessTask = null!,
                        });
                    }
                    catch (FormatException e)
                    {
                        return Results.BadRequest(new
                        {
                            e.Message
                        }); 
                    }
                    
                }
            }

            return Results.Ok();
        }));
        return app;
    }

    public static WebApplication MapAllProcessingEndpoints(this WebApplication app,
        ObservableCollection<QueueContext> queue)
    {
        var methods = typeof(MapProcessingEndpoints).GetMethods(BindingFlags.Static | BindingFlags.Public);
        foreach (var method in methods)
        {
            switch (method.Name)
            {
                case "MapAllProcessingEndpoints":
                    continue;

                case "Queue":
                case "Submit":
                    method.Invoke(null, [app, queue]);
                    continue;
                default:
                    method.Invoke(null, [app]);
                    continue;
            }
        }

        return app;
    }
}