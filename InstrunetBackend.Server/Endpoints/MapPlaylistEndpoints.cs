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
            app.MapPost("playlist-owned", (HttpContext httpContext,InstrunetDbContext context,  bool thumbnail = true ) =>
            {
                var uuidSession = httpContext.Session.GetString("uuid");
                if (string.IsNullOrEmpty(uuidSession))
                {
                    return Results.StatusCode(500);
                }

                var arr = context.Playlists.Where(i => i.Owner == uuidSession).Select(i=>new
                {
                    i.Uuid, i.Owner, i.Private, i.Title, i.Content
                }).AsEnumerable().Select(i=>new
                {
                    i.Uuid,i.Owner, i.Private, i.Title, Content = JsonSerializer.Deserialize<string[]>(i.Content)
                });
                return Results.Json(arr);
            });
            return app;
        }

        public static WebApplication MapGetPlaylist(this WebApplication app)
        {
            app.MapGet("/playlist", (string playlistUuid, InstrunetDbContext context) =>
            {
                var arr = context.Playlists.FirstOrDefault(i => i.Uuid == playlistUuid);
                if (arr == null)
                        return Results.NotFound();
                var listOfSongs = new List<dynamic>(); 
                foreach (var se in JsonSerializer.Deserialize<string[]>(arr.Content, JsonSerializerOptions.Default)??throw new NullReferenceException())
                {
                    listOfSongs.Add(context.InstrunetEntries.Select(i=>new
                    {
                        i.SongName, i.AlbumName, i.Artist, i.Kind, i.Uuid
                    }).FirstOrDefault(i => i.Uuid == se) ?? throw new FileNotFoundException());
                }
                return Results.Ok(new
                    {
                        OwnerName = context.Users.Where(u => u.Uuid == arr.Owner).Select(i => i.Username).AsEnumerable()
                            .FirstOrDefault() ?? "DELETED USER",
                        arr.Owner,
                        arr.Title,
                        playlistuuid = arr.Uuid,
                        content =  listOfSongs,
                        arr.Private
                    });
                
            });
            return app;
        }

        public static WebApplication MapGetPlaylistName(this WebApplication app)
        {
            app.MapGet("/playlist-name", (string playlistUuid, InstrunetDbContext context) =>
            {
                var res = context.Playlists.Where(i => i.Uuid == playlistUuid).Select(i => i.Title).FirstOrDefault();
                return Results.Text(res ?? null);
            });
            return app;
        }

        public static WebApplication MapGetPlaylistThumbnail(this WebApplication app)
        {
            app.MapGet("/playlist-tmb", (string playlistUuid, InstrunetDbContext context,  bool asFile = false) =>
            {
                var res = context.Playlists.Where(i => i.Uuid == playlistUuid).Select(i => i.Tmb).FirstOrDefault();
                if (asFile)
                {
                    return Results.File(res ?? [], contentType: "image/webp", enableRangeProcessing: false);
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
            app.MapPost("/upload-playlist", (HttpContext httpContext, [FromBody] PlaylistUploadContext uploadContent, InstrunetDbContext context) =>
            {
                var userUuid = httpContext.Session.GetString("uuid");
                if (string.IsNullOrWhiteSpace(userUuid))
                {
                    return Results.Unauthorized();
                }

                if (!context.Playlists.Any(i => i.Uuid == uploadContent.PlaylistUuid))
                {
                    return Results.BadRequest();
                }

                var d = JsonSerializer.Serialize(uploadContent.Content.Select(i => i.Uuid).ToArray()); 
                try
                {
                    context.Playlists.Where(i => i.Uuid == uploadContent.PlaylistUuid).ExecuteUpdate(setters =>
                        setters.SetProperty(i => i.Content, d
                                )
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
            app.MapPost("/upload-playlist-thumbnail", ([FromBody] PlaylistUploadThumbnailContext uploadContext, HttpContext httpContext, InstrunetDbContext context) =>
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
            app.MapGet("/create-playlist", (HttpContext httpContext, InstrunetDbContext context) =>
            {
                var user = httpContext.Session.GetString("uuid");
                if (string.IsNullOrWhiteSpace(user))
                {
                    return Results.Unauthorized(); 
                }

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
            app.MapPost("/remove-playlist", ([FromBody] RemovePlaylistPayload removeBody, HttpContext httpContext, InstrunetDbContext context) =>
            {
                string? uuidSession = httpContext.Session.GetString("uuid");
                if (string.IsNullOrWhiteSpace(uuidSession))
                {
                    return Results.BadRequest();
                }

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