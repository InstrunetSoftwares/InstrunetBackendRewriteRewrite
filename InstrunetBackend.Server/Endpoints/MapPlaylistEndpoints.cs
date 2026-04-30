using System.Collections.Immutable;
using System.IO.Compression;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using AXExpansion;
using CueSharp;
using InstrunetBackend.Server.Context;
using InstrunetBackend.Server.IndependantModels.HttpPayload;
using InstrunetBackend.Server.InstrunetModels;
using InstrunetBackend.Server.lib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.International.Converters.TraditionalChineseToSimplifiedConverter;
using Index = CueSharp.Index;

namespace InstrunetBackend.Server.Endpoints;

public static class MapPlaylistEndpoints
{
    enum OrderType
    {
        TimeDesc,
        TimeAsc,
        NameDesc,
        NameAsc,
    }
    public static WebApplication MapPlaylistBrowse(this WebApplication app)
    {
        
        app.MapGet("playlist-browse", (HttpContext c, InstrunetDbContext context, OrderType orderType, string? search) =>
        {
            Expression<Func<Playlist, bool>> rule = i =>  i.Content != "[]" && !i.Private && !string.IsNullOrWhiteSpace(i.Title);

            var firstStage = (orderType switch
            {
                OrderType.TimeDesc => context.Playlists.Where(rule)
                    .OrderByDescending(i => i.Modified),
                OrderType.TimeAsc => context.Playlists.Where(rule).OrderBy(i => i.Modified),
                OrderType.NameDesc => context.Playlists.Where(rule).OrderByDescending(i => i.Title),
                OrderType.NameAsc => context.Playlists.Where(rule).OrderBy(i => i.Title),
                _ => throw new ArgumentOutOfRangeException()
            }).ToList().Select(i => new
            {
                i.Uuid, Owner = context.Users.FirstOrDefault(p => p.Uuid == i.Owner), i.Title,
                Content = JsonSerializer.Deserialize<string[]>(i.Content)!.Select(p=>context.InstrunetEntries.Select(i=>new
                {
                    i.Uuid, 
                    i.SongName,
                    i.AlbumName,
                    i.Artist
                }).FirstOrDefault(a=>a.Uuid == p))
            }).Where(p => string.IsNullOrWhiteSpace(search) ||
                          InstrunetExtensions.GetChineseSearchPredicateGeneral(p.Title!, search)||
                                    InstrunetExtensions.GetChineseSearchPredicateGeneral(p.Owner?.Username ?? "", search) ||
                          p.Content.Any(c=>InstrunetExtensions.GetChineseSearchPredicateGeneral(c?.SongName ?? "", search) ||
                                           InstrunetExtensions.GetChineseSearchPredicateGeneral(c?.AlbumName??"", search) ||
                                           InstrunetExtensions.GetChineseSearchPredicateGeneral(c?.Artist??"", search))
                          );
            
            return Results.Ok(firstStage.Select(p=>new  {p.Uuid, p.Title, Content=  p.Content.Select(i=>i?.Uuid),  Owner = p.Owner?.Uuid}));
        });
        return app; 
    }
    public static WebApplication MapGetPlaylistOwned(this WebApplication app)
    {
        app.MapPost("playlist-owned", (HttpContext httpContext, InstrunetDbContext context, bool thumbnail = true) =>
        {
            var uuidSession = httpContext.Session.GetString("uuid");
            if (string.IsNullOrEmpty(uuidSession)) return Results.StatusCode(500);

            var arr = context.Playlists.Where(i => i.Owner == uuidSession).Select(i => new
            {
                i.Uuid, i.Owner, i.Private, i.Title, i.Content
            }).AsEnumerable().Select(i => new
            {
                i.Uuid, i.Owner, i.Private, i.Title, Content = JsonSerializer.Deserialize<string[]>(i.Content)
            });
            return Results.Json(arr);
        });
        return app;
    }

    public static WebApplication MapGetPlaylist(this WebApplication app)
    {
        app.MapGet("/playlist", (string playlistUuid, InstrunetDbContext context, ILogger<WebApplication> logger) =>
        {
            var arr = context.Playlists.FirstOrDefault(i => i.Uuid == playlistUuid);
            if (arr == null)
                return Results.NotFound();
            var listOfSongs = new List<dynamic>();
            var deserialized = JsonSerializer.Deserialize<string[]>(arr.Content, JsonSerializerOptions.Default) ??
                               throw new NullReferenceException();

            foreach (var se in deserialized)
            {
                var song = context.InstrunetEntries.Select(i => new
                {
                    i.SongName, i.AlbumName, i.Artist, i.Kind, i.Uuid
                }).FirstOrDefault(i => i.Uuid == se);
                if (song is null)
                {
                    logger.LogWarning(
                        "Error on processing playlist: {0}; Skipping this song ({1}) on response and removed this entry in DB. ",
                        playlistUuid, se);
                    var clone = deserialized.ToList();
                    clone.Remove(p => p == se);
                    context.Playlists.Where(i=>i.Uuid == playlistUuid).ExecuteUpdate(s => s.SetProperty(p => p.Content, clone.Serialize()));
                    continue;
                }

                listOfSongs.Add(song);
            }

            return Results.Ok(new
            {
                OwnerName = context.Users.Where(u => u.Uuid == arr.Owner).Select(i => i.Username).AsEnumerable()
                    .FirstOrDefault() ?? "DELETED USER",
                arr.Owner,
                arr.Title,
                playlistuuid = arr.Uuid,
                content = listOfSongs,
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
        app.MapGet("/playlist-tmb", (string playlistUuid, InstrunetDbContext context, bool asFile = false) =>
        {
            var res = context.Playlists.Where(i => i.Uuid == playlistUuid).Select(i => i.Tmb).FirstOrDefault();
            if (asFile) return Results.File(res ?? [], "image/webp", enableRangeProcessing: true);

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
        app.MapPost("/upload-playlist", (HttpContext httpContext, [FromBody] PlaylistUploadContext uploadContent,
            InstrunetDbContext context) =>
        {
            var userUuid = httpContext.Session.GetString("uuid");
            if (string.IsNullOrWhiteSpace(userUuid)) return Results.Unauthorized();

            if (!context.Playlists.Any(i => i.Uuid == uploadContent.PlaylistUuid) || uploadContent.Title.IsNullOrWhiteSpace()) return Results.BadRequest();
            var d = JsonSerializer.Serialize(uploadContent.Content.Select(i => i.Uuid).ToArray());
            try
            {
                context.Playlists.Where(i => i.Uuid == uploadContent.PlaylistUuid).ExecuteUpdate(setters =>
                    setters.SetProperty(i => i.Content, d
                        )
                        .SetProperty(i => i.Title, uploadContent.Title)
                        .SetProperty(i => i.Private, uploadContent.Private)
                        .SetProperty(i=>i.Modified, DateTime.Now)
                );
                return Results.Ok();
            }
            catch (Exception)
            {
                return Results.InternalServerError();
            }
        });
        app.MapPost("/upload-playlist-thumbnail", ([FromBody] PlaylistUploadThumbnailContext uploadContext,
            HttpContext httpContext, InstrunetDbContext context) =>
        {
            var userUuid = httpContext.Session.GetString("uuid");
            if (string.IsNullOrWhiteSpace(userUuid)) return Results.Unauthorized();
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
                        .SetProperty(i=>i.Modified, DateTime.Now)
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
            if (string.IsNullOrWhiteSpace(user)) return Results.Unauthorized();

            var uuid = Guid.NewGuid().ToString();
            context.Playlists.Add(new Playlist
            {
                Uuid = uuid, Content = "[]", Owner = user, Private = false, Title = "未命名播放列表", Tmb = null, Created = DateTime.Now
            });
            context.SaveChanges();
            return Results.Text(uuid);
        });
        return app;
    }

    public static WebApplication MapRemovePlaylist(this WebApplication app)
    {
        app.MapPost("/remove-playlist",
            ([FromBody] RemovePlaylistPayload removeBody, HttpContext httpContext, InstrunetDbContext context) =>
            {
                var uuidSession = httpContext.Session.GetString("uuid");
                if (string.IsNullOrWhiteSpace(uuidSession)) return Results.BadRequest();

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

    public static WebApplication MapDownloadPlaylistToCue(this WebApplication app)
    {
        app.MapGet("/playlist-cue",
            (ILogger<WebApplication> log, HttpContext context, InstrunetDbContext db, string uuid) =>
            {
                var dbPlaylist = db.Playlists.FirstOrDefault(i => i.Uuid == uuid);
                if (dbPlaylist is null) return Results.NotFound();
                var playlist =
                    JsonSerializer.Deserialize<string[]>(dbPlaylist.Content);
                if (playlist is null)
                {
                    log.LogError("Failed to deserialize playlist: {0}", dbPlaylist.Uuid);
                    return Results.InternalServerError("Failed to deserialize playlist");
                }

                IEnumerable<InstrunetEntry> F()
                {
                    foreach (var se in playlist)
                        if (db.InstrunetEntries.Find(se) is { } e)
                        {
                            yield return e;
                        }
                        else
                        {
                            log.LogWarning(
                                "Failed to retrieve song {0} according to playlist {1}; Now removing this entry from playlist.",
                                se, dbPlaylist.Uuid);
                            db.Playlists.Find(uuid)?.Content = playlist.ToList().RemoveAll(p => p == se).Serialize();
                            db.SaveChanges();
                        }
                }

                var originals = F().ToImmutableList();
                using var archiveStream = new MemoryStream();
                var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, true);
                var cue = new CueSheet();
                cue.Title = dbPlaylist.Title;
                for (var index = 0; index < originals.Count; index++)
                {
                    var instrunetEntry = originals[index];
                    using var streamAudio =
                        archive.CreateEntry($"{instrunetEntry.Uuid[..5]}_{instrunetEntry.SongName}.mp3",
                            CompressionLevel.SmallestSize).Open();
                    if (instrunetEntry.Databinary is null)
                    {
                        log.LogError("DataBinary is null for: {0}", instrunetEntry.Uuid);
                        return Results.InternalServerError();
                    }

                    using var temp = new MemoryStream(instrunetEntry.Databinary);
                    temp.CopyTo(streamAudio);

                    cue.AddTrack(new Track
                    {
                        Comments =
                        [
                            instrunetEntry.Uuid
                        ],
                        DataFile = new AudioFile($"{instrunetEntry.Uuid[..5]}_{instrunetEntry.SongName}.mp3",
                            FileType.MP3),
                        Garbage = new string[]
                        {
                        },
                        Indices =
                        [
                            new Index
                            {
                                Frames = 0, Minutes = 0, Number = 1, Seconds = 0
                            }
                        ],
                        ISRC = "",
                        Performer = $"{instrunetEntry.Artist}",
                        Songwriter = $"{instrunetEntry.Artist}",
                        Title = $"{instrunetEntry.SongName}",
                        TrackDataType = DataType.AUDIO,
                        TrackFlags = new Flags[]
                        {
                        },
                        TrackNumber = index + 1
                    });
                }

                var cueTempPath = Path.GetTempFileName();
                cue.SaveCue(cueTempPath, new UTF8Encoding());
                var fileLines = File.ReadAllLines(cueTempPath).ToList();
                File.Delete(cueTempPath);
                fileLines.Remove(p => p.Trim() switch
                {
                    { } s => s.StartsWith("PREGAP") || s.StartsWith("POSTGAP") || s.StartsWith("INDEX 00")
                });
                using (var cueArchive = archive.CreateEntry($"{dbPlaylist.Title}.cue", CompressionLevel.SmallestSize)
                           .Open())
                {
                    using var writer = new StreamWriter(cueArchive, Encoding.UTF8);
                    foreach (var fileLine in fileLines) writer.Write($"{fileLine}\n");
                }

                archive.Dispose();
                return Results.File(archiveStream.ToArray(), fileDownloadName: $"{dbPlaylist.Title}.zip");
            });
        return app;
    }

    public static WebApplication MapAllPlaylistEndpoints(this WebApplication app)
    {
        var methodInfos =
            typeof(MapPlaylistEndpoints).GetMethods(BindingFlags.Static |
                                                    BindingFlags.Public);
        foreach (var methodInfo in methodInfos)
            switch (methodInfo.Name)
            {
                case "MapAllPlaylistEndpoints":
                    continue;
                default:
                    methodInfo.Invoke(null, [app]);
                    break;
            }

        return app;
    }
}