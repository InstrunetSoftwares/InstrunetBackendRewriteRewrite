using InstrunetBackend.Server.Context;
using InstrunetBackend.Server.IndependantModels.HttpPayload;
using InstrunetBackend.Server.InstrunetModels;
using InstrunetBackend.Server.lib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Text.Json;
using WebPWrapper.Encoder;

namespace InstrunetBackend.Server.Endpoints
{
    public static class MapPlaylistEndpoints
    {
        public static WebApplication MapGetPlaylistOwned(this WebApplication app)
        {
            
            app.MapPost("playlist-owned", (HttpContext httpContext, bool thumbnail = true) =>
            {
                var uuidSession = httpContext.Session.GetString("uuid");
                if (string.IsNullOrEmpty(uuidSession))
                {
                    return Results.StatusCode(500);
                }

                using var context = new InstrunetDbContext();
                var arr = context.Playlists.Where(i => i.Owner == uuidSession);
                List<object> sendObj = [];
                foreach (var obj in arr)
                {
                    sendObj.Add(new
                    {
                        owner = obj.Owner,
                        @private = obj.Private,
                        title = obj.Title,
                        tmb = thumbnail ? new
                        {
                            type = "Buffer",
                            data = obj.Tmb?.Select(b => (int)b).ToArray()
                        } : null,
                        uuid = obj.Uuid,
                        content = JsonSerializer.Deserialize<string[]>(obj.Content)
                    });
                }
                //var intArray = arr!.Select(b => (int)b).ToArray();

                return Results.Json(sendObj);
            });
            return app;
        }
        public static WebApplication MapGetPlaylist(this WebApplication app)
        {
            app.MapGet("/playlist", (string playlistUuid) =>
            {
                using var context = new InstrunetDbContext();
                var arr = context.Playlists.Where(i => i.Uuid == playlistUuid).Select(i => new
                {
                    owner = i.Owner,
                    uuid = i.Uuid,
                    content = JsonSerializer.Deserialize<string[]>(i.Content, JsonSerializerOptions.Default),
                    @private = i.Private
                }).FirstOrDefault();
                return Results.Json(arr);
            });
            return app;
        }
        public static WebApplication MapGetPlaylistName(this WebApplication app)
        {
            app.MapGet("/playlist-name", (string playlistUuid) =>
            {
                using var context = new InstrunetDbContext();
                var res = context.Playlists.Where(i => i.Uuid == playlistUuid).Select(i => i.Title).FirstOrDefault();
                return Results.Ok(res ?? null);
            });
            return app;
        }
        public static WebApplication MapGetPlaylistThumbnail(this WebApplication app)
        {
            app.MapGet("/playlist-tmb", (string playlistUuid) =>
            {
                using var context = new InstrunetDbContext();
                var res = context.Playlists.Where(i => i.Uuid == playlistUuid).Select(i => i.Tmb).FirstOrDefault();
                var arr = res?.Select(b => (int)b).ToArray();

                return Results.Json(new
                {
                    tmb = res == null
                        ? null
                        : new
                        {
                            type = "Buffer",
                            data = arr
                        }
                });
            });
            return app;
        }
        public static WebApplication MapUploadPlaylist(this WebApplication app)
        {
            app.MapPost("/upload-playlist", (HttpContext httpContext, [FromBody] PlaylistUploadContext uploadContent) =>
            {
                string? uuidSession = httpContext.Session.GetString("uuid");
                if (uuidSession == null)
                {
                    return Results.BadRequest();
                }

                using var context = new InstrunetDbContext();
                if (string.IsNullOrEmpty(uploadContent.Playlistuuid))
                {
                    string uuid = Guid.NewGuid().ToString();
                    if (uploadContent.TmbInstance?.Data != null)
                    {
                        List<byte> thumbBytes = new();
                        foreach (int byteInt in uploadContent.TmbInstance.Data)
                        {
                            try
                            {
                                thumbBytes.Add((byte)byteInt);

                            }
                            catch (OverflowException ex)
                            {
                                Console.WriteLine(ex);
                                return Results.BadRequest("Invalid thumbnail data: " + ex);
                            }
                        }
                        var builder = LibraryHelper.CreateWebPEncoderBuilder();

                        if (builder is null)
                        {
                            goto skip_compression;
                        }
                        var encoder = builder.CompressionConfig(x => x.Lossless(y => y.Quality(80))).Build();
                        var inputStream = new MemoryStream();

                        try
                        {
                            foreach (int byteInt in uploadContent.TmbInstance.Data)
                            {
                                inputStream.WriteByte((byte)byteInt);

                            }
                        }
                        catch (OverflowException ex)
                        {
                            Console.WriteLine(ex);

                            return Results.BadRequest("Invalid thumbnail data: " + ex);
                        }
                        finally
                        {
                            inputStream.Dispose();
                        }

                        var outputStream = new MemoryStream();
                        try
                        {
                            encoder.Encode(inputStream, outputStream);
                            thumbBytes = outputStream.ToArray().ToList();

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                            return Results.BadRequest("Unknown error: " + ex.Message);
                        }
                        finally
                        {
                            inputStream.Dispose();
                            outputStream.Dispose();
                        }




                    skip_compression:

                        //Add
                        context.Playlists.Add(new Playlist
                        {
                            Uuid = uuid,
                            Content = JsonSerializer.Serialize(uploadContent.Content, JsonSerializerOptions.Default),
                            Private = uploadContent.Private,
                            Title = uploadContent.Title,
                            Owner = uuidSession,
                            Tmb = thumbBytes.ToArray()
                        });
                        context.SaveChanges();
                        return Results.Json(JsonSerializer.Serialize(new
                        {
                            UUID = uuid
                        }, new JsonSerializerOptions { PropertyNameCaseInsensitive = false }));
                    }



                    //Add
                    context.Playlists.Add(new Playlist
                    {
                        Uuid = uuid,
                        Content = JsonSerializer.Serialize(uploadContent.Content, JsonSerializerOptions.Default),
                        Private = uploadContent.Private,
                        Title = uploadContent.Title,
                        Owner = uuidSession,
                    });
                    context.SaveChanges();
                    return Results.Json(JsonSerializer.Serialize(new
                    {
                        UUID = uuid
                    }, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }));
                }
                else
                {
                    var res = context.Playlists.Where(i => i.Uuid == uploadContent.Playlistuuid).Select(i => i.Owner)
                        .FirstOrDefault();
                    if (res != uuidSession)
                    {
                        return Results.Unauthorized();
                    }

                    string uuid = Guid.NewGuid().ToString();
                    if (uploadContent.TmbInstance?.Data != null)
                    {
                        List<byte> encodeOutputBytes = new();
                        try
                        {
                            foreach (int byteInt in uploadContent.TmbInstance.Data)
                            {
                                encodeOutputBytes.Add((byte)byteInt);
                            }
                        }
                        catch (OverflowException ex)
                        {
                            Console.WriteLine(ex);
                            return Results.BadRequest("Invalid thumbnail data: " + ex.Message);
                        }
                        var builder = LibraryHelper.CreateWebPEncoderBuilder();
                        if (builder is null)
                        {
                            goto skip_compression;
                        }
                        var encoder = builder.CompressionConfig(x => x.Lossless(y => y.Quality(80))).Build();
                        var inputStream = new MemoryStream();
                        try
                        {
                            foreach (int byteInt in uploadContent.TmbInstance.Data)
                            {
                                inputStream.WriteByte((byte)byteInt);
                            }
                        }
                        catch (OverflowException ex)
                        {
                            Console.WriteLine(ex);
                            return Results.BadRequest("Invalid thumbnail data: " + ex.Message);
                        }
                        finally
                        {
                            inputStream.Dispose();
                        }

                        var outputStream = new MemoryStream();
                        try
                        {
                            encoder.Encode(inputStream, outputStream);
                            encodeOutputBytes = outputStream.ToArray().ToList();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                            return Results.BadRequest("Unknown error: " + ex.Message);
                        }
                        finally
                        {
                            inputStream.Dispose();
                            outputStream.Dispose();

                        }

                    skip_compression:
                        context.Playlists.Where(i => i.Uuid == uploadContent.Playlistuuid).ExecuteUpdate(setters =>
                            setters
                                .SetProperty(i => i.Content,
                                    JsonSerializer.Serialize(uploadContent.Content, JsonSerializerOptions.Default))
                                .SetProperty(i => i.Private, uploadContent.Private)
                                .SetProperty(i => i.Tmb, encodeOutputBytes.ToArray())
                                .SetProperty(i => i.Title, uploadContent.Title));
                        return Results.Ok();
                    }




                    context.Playlists.Where(i => i.Uuid == uploadContent.Playlistuuid).ExecuteUpdate(setters =>
                        setters
                            .SetProperty(i => i.Content,
                                JsonSerializer.Serialize(uploadContent.Content, JsonSerializerOptions.Default))
                            .SetProperty(i => i.Private, uploadContent.Private)
                            .SetProperty(i => i.Title, uploadContent.Title));
                    return Results.Ok();
                }
            });
            return app;
        }
        public static WebApplication MapRemovePlaylist(this WebApplication app)
        {
            app.MapPost("/remove-playlist", ([FromBody] RemovePlaylistPayload removeBody, HttpContext httpContext) =>
            {
                string? uuidSession = httpContext.Session.GetString("uuid");
                if (string.IsNullOrWhiteSpace(uuidSession))
                {
                    return Results.BadRequest(); 
                }

                using var context = new InstrunetDbContext();
                var ownerUuid = context.Playlists.Where(i => i.Uuid == removeBody.Playlistuuid).Select(i => i.Owner)
                    .FirstOrDefault();
                if (ownerUuid == uuidSession)
                {
                    context.Playlists.Where(i => i.Uuid == removeBody.Playlistuuid).ExecuteDelete();
                    return Results.Ok(); 
                }

                return Results.Unauthorized(); 
            }); 
            return app; 
        }
        public static WebApplication MapAllPlaylistEndpoints(this WebApplication app)
        {
            var methodInfos = typeof(MapPlaylistEndpoints).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public); 
            foreach(var methodInfo in methodInfos)
            {
                switch (methodInfo.Name)
                {
                    case "MapAllPlaylistEndpoints":
                        continue;
                    default:
                        methodInfo.Invoke(null, [app]);
                        break; 
                }
            }
            return app;
        }
    }
}
