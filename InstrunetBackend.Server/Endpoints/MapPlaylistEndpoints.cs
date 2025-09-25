using InstrunetBackend.Server.Context;
using InstrunetBackend.Server.IndependantModels.HttpPayload;
using InstrunetBackend.Server.InstrunetModels;
using InstrunetBackend.Server.lib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
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
                        tmb = thumbnail
                            ? new
                            {
                                type = "Buffer",
                                data = obj.Tmb?.Select(b => (int)b).ToArray()
                            }
                            : null,
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
                    OwnerName = context.Users.Where(u => u.Uuid == i.Owner).Select(i => i.Username).AsEnumerable().FirstOrDefault() ?? "DELETED USER",
                    i.Owner,
                    i.Title,
                    playlistuuid = i.Uuid,
                    content = JsonSerializer.Deserialize<string[]>(i.Content, JsonSerializerOptions.Default),
                    i.Private
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
                return Results.Text(res ?? null);
            });
            return app;
        }

        public static WebApplication MapGetPlaylistThumbnail(this WebApplication app)
        {
            app.MapGet("/playlist-tmb", (string playlistUuid, bool asFile = false) =>
            {
                using var context = new InstrunetDbContext();
                var res = context.Playlists.Where(i => i.Uuid == playlistUuid).Select(i => i.Tmb).FirstOrDefault();
                if (asFile)
                {
                    return Results.File(res ?? [], contentType: "image/webp", enableRangeProcessing: true);
                }

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
                var userUuid = httpContext.Session.GetString("uuid");
                if (string.IsNullOrWhiteSpace(userUuid))
                {
                    return Results.Unauthorized();
                }

                using var context = new InstrunetDbContext();
                if (!context.Playlists.Any(i => i.Uuid == uploadContent.PlaylistUuid))
                {
                    return Results.BadRequest();
                }

                try
                {
                    context.Playlists.Where(i => i.Uuid == uploadContent.PlaylistUuid).ExecuteUpdate(setters =>
                        setters.SetProperty(i => i.Content,
                                JsonSerializer.Serialize(uploadContent.Content, JsonSerializerOptions.Web))
                            .SetProperty(i => i.Title, uploadContent.Title)
                            .SetProperty(i => i.Private, uploadContent.Private)
                    );
                    return Results.Ok();
                }
                catch (Exception)
                {
                    return Results.InternalServerError();
                }
            });
            app.MapPost("/upload-playlist-thumbnail", ([FromBody] PlaylistUploadThumbnailContext uploadContext, HttpContext httpContext) =>
            {
                var userUuid = httpContext.Session.GetString("uuid");
                if (string.IsNullOrWhiteSpace(userUuid))
                {
                    return Results.Unauthorized();
                }
                byte[] buffer; 
                try
                {
                    buffer = uploadContext.DataUri.DataUrlToByteArray();

                }
                catch (Exception)
                {
                    return Results.BadRequest("文件不支持或不合法"); 
                }
                #region compression
                var builder = LibraryHelper.CreateWebPEncoderBuilder();
                try
                {
                    if (builder is not null)
                    {
                        var encoder = builder.CompressionConfig(x => x.Lossy(y => y.Quality(80).Size(100000))).Build();
                        using var input = new MemoryStream(buffer);
                        using var output = new MemoryStream();
                        encoder.Encode(input, output);
                        buffer = output.ToArray();
                    }
                }
                catch (Exception e)
                {
                    return Results.BadRequest("文件不支持或不合法: " + e); 
                }
               
               


                #endregion
                using var context = new InstrunetDbContext();
                try
                {
                    context.Playlists.Where(i => i.Uuid == uploadContext.PlaylistUuid).ExecuteUpdate(setters =>
                        setters.SetProperty(i => i.Tmb, buffer)
                    );
                    return Results.Ok();
                }
                catch (Exception)
                {
                    return Results.InternalServerError();
                }
            });
            app.MapGet("/create-playlist", (HttpContext httpContext) =>
            {
                var user = httpContext.Session.GetString("uuid");
                if (string.IsNullOrWhiteSpace(user))
                {
                    return Results.Unauthorized(); 
                }

                using var context = new InstrunetDbContext();
                var uuid = Guid.NewGuid().ToString(); 
                context.Playlists.Add(new()
                {
                    Uuid = uuid, Content = "[]", Owner = user, Private = false, Title = null, Tmb = null 
                });
                context.SaveChanges(); 
                return Results.Text(uuid); 
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
            var methodInfos =
                typeof(MapPlaylistEndpoints).GetMethods(System.Reflection.BindingFlags.Static |
                                                        System.Reflection.BindingFlags.Public);
            foreach (var methodInfo in methodInfos)
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