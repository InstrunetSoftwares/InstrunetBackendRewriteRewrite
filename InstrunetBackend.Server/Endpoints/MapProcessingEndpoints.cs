using System.Collections.ObjectModel;
using System.Net;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using InstrunetBackend.Server.Context;
using InstrunetBackend.Server.IndependantModels;
using InstrunetBackend.Server.IndependantModels.HttpPayload;
using InstrunetBackend.Server.lib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.International.Converters.TraditionalChineseToSimplifiedConverter;
using Newtonsoft.Json;
using WebPWrapper.Encoder;

namespace InstrunetBackend.Server.Endpoints;

internal static class MapProcessingEndpoints
{
    private static Func<(SubmitContext?, SubmitContext<IFormFile>?), string?, IResult>? _handler;

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
        _handler = (submitContext, userUuid) =>
        {   
            
            if(submitContext.Item1 is null)
            {
                
                var sub = submitContext.Item2!; 
                if(sub.kind.Length == 0)
                {
                    return Results.BadRequest(); 
                }
                if (sub.albumName is null || string.IsNullOrEmpty(sub.albumName.Trim()))
                {
                    sub.albumName = "未知专辑";
                }

                if (sub.artist is null || string.IsNullOrEmpty(sub.artist.Trim()))
                {
                    sub.artist = "未知艺术家";
                }

                if (string.IsNullOrEmpty(sub.name.Trim()))
                {
                    return Results.BadRequest(new
                    {
                        Message = "文件不存在"
                    });
                }

                var kind = sub.kind[0]; 
                    using var context = new InstrunetDbContext();

                    #region rep1

                    var rep = context.InstrunetEntries.Count(i => ((i.SongName == (sub.name) &&
                                                                    i.Artist == (sub.artist)) ||
                                                                   (i.SongName == (
                                                                        ChineseConverter.Convert(sub
                                                                                .name,
                                                                            ChineseConversionDirection
                                                                                .SimplifiedToTraditional)) &&
                                                                    i.Artist == (
                                                                        ChineseConverter.Convert(sub
                                                                                .artist,
                                                                            ChineseConversionDirection
                                                                                .SimplifiedToTraditional))) ||
                                                                   (i.SongName == (
                                                                        ChineseConverter.Convert(sub
                                                                                .name,
                                                                            ChineseConversionDirection
                                                                                .TraditionalToSimplified)) &&
                                                                    i.Artist == (
                                                                        ChineseConverter.Convert(sub
                                                                                .artist,
                                                                            ChineseConversionDirection
                                                                                .TraditionalToSimplified))
                                                                   )) && i.Kind == kind);

                    #endregion

                    #region rep2

                    var rep2 = queue.Count(i => ((i.Name == (sub.name) &&
                                                  i.Artist == (sub.artist)) ||
                                                 (i.Name == (
                                                      ChineseConverter.Convert(sub
                                                              .name,
                                                          ChineseConversionDirection
                                                              .SimplifiedToTraditional)) &&
                                                  i.Artist == (
                                                      ChineseConverter.Convert(sub
                                                          .artist, ChineseConversionDirection.SimplifiedToTraditional))) ||
                                                 (i.Name == (
                                                      ChineseConverter.Convert(sub
                                                          .name, ChineseConversionDirection.TraditionalToSimplified)) &&
                                                  i.Artist == (
                                                      ChineseConverter.Convert(sub
                                                          .artist, ChineseConversionDirection.TraditionalToSimplified))
                                                 )) && i.Kind == kind);

                    #endregion

                    if (rep != 0 || rep2 != 0) return Results.InternalServerError("已在数据库中存在");


                    if (sub.albumCover is null)
                    {
                        try
                        {

                            using var mStream = new MemoryStream();
                            sub.fileBinary.CopyTo(mStream);
                            
                            queue.Add(new()
                            {
                                Uuid = Guid.NewGuid()
                                .ToString(),
                                Name = sub.name,
                                Artist = sub.artist,
                                AlbumName = sub.albumName,
                                Kind = kind,
                                Email = sub.email,
                                Link = sub.link,
                                UserUuid = userUuid,
                                AlbumCover = null,
                                DateTimeUploaded = DateTime.Now,
                                CancellationToken = new CancellationTokenSource(),
                                File = mStream.ToArray(),
                                ProcessTask = null!,
                            });
                            return Results.Ok();

                        }
                        catch (FormatException e)
                        {
                            return Results.BadRequest(new
                            {
                                e.Message
                            });
                        }
                    }

                    var builder = LibraryHelper.CreateWebPEncoderBuilder(); 

                    if (builder is null)
                    {
                        try
                        {

                            using var mStream = new MemoryStream();
                            sub.fileBinary.CopyTo(mStream);
                            using var albumPicture = new MemoryStream();
                            sub.albumCover.CopyTo(albumPicture); 
                            queue.Add(new()
                            {
                                Uuid = Guid.NewGuid()
                                .ToString(),
                                Name = sub.name,
                                Artist = sub.artist,
                                AlbumName = sub.albumName,
                                Kind = kind,
                                Email = sub.email,
                                Link = sub.link,
                                UserUuid = userUuid,
                                AlbumCover = albumPicture.ToArray(),
                                DateTimeUploaded = DateTime.Now,
                                CancellationToken = new CancellationTokenSource(),
                                File = mStream.ToArray(),
                                ProcessTask = null!,
                            });
                            return Results.Ok();

                        }
                        catch (FormatException e)
                        {
                            return Results.BadRequest(new
                            {
                                e.Message
                            });
                        }
                    }

                    


                    try
                    {

                            var encoder = builder.CompressionConfig(x => x.Lossy(y => y.Quality(80).Size(100000))).Build();
                            using var input = new MemoryStream();
                            sub.albumCover.CopyTo(input);
                    input.Position = 0; 
                            using var output = new MemoryStream();
                            encoder.Encode(input, output);
                           
                            using var mStream = new MemoryStream();
                            sub.fileBinary.CopyTo(mStream);
                            queue.Add(new()
                            {
                                Uuid = Guid.NewGuid()
                                .ToString(),
                                Name = sub.name,
                                Artist = sub.artist,
                                AlbumName = sub.albumName,
                                Kind = kind,
                                Email = sub.email,
                                Link = sub.link,
                                UserUuid = userUuid,
                                AlbumCover = output.ToArray(),
                                DateTimeUploaded = DateTime.Now,
                                CancellationToken = new CancellationTokenSource(),
                                File = mStream.ToArray(),
                                ProcessTask = null!,
                            });

                        return Results.Ok();
                    }
                    catch (Exception e)
                    {
                        return Results.BadRequest(new
                        {
                            e.Message
                        });
                    }
                

                
            }else
            {
                var sub = submitContext.Item1!;
                if(sub.kind.Length == 0)
                {
                    return Results.BadRequest(); 
                }
                if (sub.albumName is null || string.IsNullOrEmpty(sub.albumName.Trim()))
                {
                    sub.albumName = "未知专辑";
                }

                if (sub.artist is null || string.IsNullOrEmpty(sub.artist.Trim()))
                {
                    sub.artist = "未知艺术家";
                }

                if (string.IsNullOrEmpty(sub.name.Trim()))
                {
                    return Results.BadRequest(new
                    {
                        Message = "文件不存在"
                    });
                }

                var kind = sub.kind[0]; 
                    using var context = new InstrunetDbContext();

                    #region rep1

                    var rep = context.InstrunetEntries.Count(i => ((i.SongName == (sub.name) &&
                                                                    i.Artist == (sub.artist)) ||
                                                                   (i.SongName == (
                                                                        ChineseConverter.Convert(sub
                                                                                .name,
                                                                            ChineseConversionDirection
                                                                                .SimplifiedToTraditional)) &&
                                                                    i.Artist == (
                                                                        ChineseConverter.Convert(sub
                                                                                .artist,
                                                                            ChineseConversionDirection
                                                                                .SimplifiedToTraditional))) ||
                                                                   (i.SongName == (
                                                                        ChineseConverter.Convert(sub
                                                                                .name,
                                                                            ChineseConversionDirection
                                                                                .TraditionalToSimplified)) &&
                                                                    i.Artist == (
                                                                        ChineseConverter.Convert(sub
                                                                                .artist,
                                                                            ChineseConversionDirection
                                                                                .TraditionalToSimplified))
                                                                   )) && i.Kind == kind);

                    #endregion

                    #region rep2

                    var rep2 = queue.Count(i => ((i.Name == (sub.name) &&
                                                  i.Artist == (sub.artist)) ||
                                                 (i.Name == (
                                                      ChineseConverter.Convert(sub
                                                              .name,
                                                          ChineseConversionDirection
                                                              .SimplifiedToTraditional)) &&
                                                  i.Artist == (
                                                      ChineseConverter.Convert(sub
                                                          .artist, ChineseConversionDirection.SimplifiedToTraditional))) ||
                                                 (i.Name == (
                                                      ChineseConverter.Convert(sub
                                                          .name, ChineseConversionDirection.TraditionalToSimplified)) &&
                                                  i.Artist == (
                                                      ChineseConverter.Convert(sub
                                                          .artist, ChineseConversionDirection.TraditionalToSimplified))
                                                 )) && i.Kind == kind);

                    #endregion

                    if (rep != 0 || rep2 != 0) return Results.InternalServerError("已在数据库中存在");

                    #region coverProcess

                    if (sub.albumCover is null)
                    {
                        goto skip_compression;
                    }

                    var builder = LibraryHelper.CreateWebPEncoderBuilder(); 

                    if (builder is null)
                    {
                        goto skip_compression;
                    }

                    var encoder = builder.CompressionConfig(x => x.Lossy(y => y.Quality(80).Size(100000))).Build();
                    var input = new MemoryStream(sub.albumCover.DataUrlToByteArray());
                    var output = new MemoryStream();
                    encoder.Encode(input, output);
                    sub.albumCover = "data:image/webp;base64," + Convert.ToBase64String(output.ToArray());
                    output.Dispose();
                    input.Dispose();
            
                    

                #endregion

                skip_compression:
                    try
                    {
                        

                        
                        queue.Add(new()
                        {
                            Uuid = Guid.NewGuid()
                            .ToString(),
                            Name = sub.name,
                            Artist = sub.artist,
                            AlbumName = sub.albumName,
                            Kind = kind,
                            Email = sub.email,
                            Link = sub.link,
                            UserUuid = userUuid,
                            AlbumCover = sub.albumCover?.DataUrlToByteArray(),
                            DateTimeUploaded = DateTime.Now,
                            CancellationToken = new CancellationTokenSource(),
                            File = sub.fileBinary,
                            ProcessTask = null!,
                        });
                        return Results.Ok();



                    }
                    catch (FormatException e)
                        {
                            return Results.BadRequest(new
                            {
                                e.Message
                            });
                        }
                    }

            
            
        };
        app.MapPost("/submit", new Func<SubmitContext<IFormFile>, HttpContext?, IResult>(([FromForm] body, context) => _handler((null, body), context?.Session.GetString("uuid")))).DisableAntiforgery();
        return app;
    }

    public static WebApplication NcmUrl(this WebApplication app)
    {
        app.MapPost("/ncm/url", async ([FromBody] NcmUrlContext body, HttpContext context) =>
        {
            HttpClientHandler? handler = null;
            HttpClient? client = null;
            try
            {
                handler = new HttpClientHandler();
                handler.UseCookies = false;
                client = new HttpClient(handler);
                client.BaseAddress = new Uri("http://localhost:3958");
                using var message = new HttpRequestMessage(HttpMethod.Get,
                    "/song/download/url/v1?id=" + body.Id + "&level=hires");
                // 

                {
                    var stream = Assembly.GetExecutingAssembly()
                        .GetManifestResourceStream("InstrunetBackend.Server.NcmSecret");
                    var memStream = new MemoryStream();
                    await stream!.CopyToAsync(memStream);
                    await stream.DisposeAsync();
                    var secret = Encoding.UTF8.GetString(memStream.ToArray());
                    await memStream.DisposeAsync();
                    message.Headers.Add("Cookie", secret);
                }
                var res = await client.SendAsync(message);
                if (res.IsSuccessStatusCode || res.StatusCode == HttpStatusCode.NotModified)
                {
                    dynamic? end = JsonConvert.DeserializeObject<dynamic>(await res.Content.ReadAsStringAsync());
                    if (end?.data.url != null)
                    {
                        dynamic info = JsonConvert.DeserializeObject(
                            await client.GetStringAsync(
                                "http://localhost:3958/song/detail?ids=" + body.Id))!;
                        return _handler!((new SubmitContext
                        {
                            fileBinary = await client.GetByteArrayAsync((string)end.data.url),
                            name = (string)info.songs[0].name,
                            artist = (string)info.songs[0].ar[0].name,
                            albumName = (string)info.songs[0].al.name,
                            albumCover = "data:image/webp;base64," +
                                         Convert.ToBase64String(
                                             await client.GetByteArrayAsync((string)info.songs[0].al.picUrl)),
                            email = body.Email,
                            kind = body.Kind,
                            link = (string)end.data.url
                        }, null), context.Session.GetString("uuid"));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                handler?.Dispose();
                client?.Dispose();
            }

            return Results.BadRequest("不存在或需要付费");
        });
        return app;
    }

    
    public static WebApplication MapAllProcessingEndpoints(this WebApplication app,
        ObservableCollection<QueueContext> queue)
    {
        var methods = typeof(MapProcessingEndpoints).GetMethods(BindingFlags.Static | BindingFlags.Public);
        if (methods.Where(i => i.Name == "Submit").Select(i => i.Name).First() == "Submit")
        {
            methods.First(i => i.Name == "Submit").Invoke(null, [app, queue]);
        }

        foreach (var method in methods)
        {
            switch (method.Name)
            {
                case "MapAllProcessingEndpoints":
                    continue;

                case "NcmUrl":
                    method.Invoke(null, [app]);
                    break; 
                case "Queue":
                    method.Invoke(null, [app, queue]);
                    continue;
                case "Submit":
                    continue;


                default:
                    method.Invoke(null, [app]);
                    continue;
            }
        }

        return app;
    }
}