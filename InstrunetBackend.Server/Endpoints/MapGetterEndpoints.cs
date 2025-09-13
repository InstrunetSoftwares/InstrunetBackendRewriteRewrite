using System.Collections.ObjectModel;
using System.Reflection;
using InstrunetBackend.Server.Context;
using InstrunetBackend.Server.IndependantModels;
using InstrunetBackend.Server.IndependantModels.HttpPayload;
using Microsoft.AspNetCore.Mvc;
using Microsoft.International.Converters.TraditionalChineseToSimplifiedConverter;

namespace InstrunetBackend.Server.Endpoints;

public static class MapGetterEndpoints
{
    public static WebApplication SongAndPitching(this WebApplication app, List<QueueContext> cache)
    {
        app.MapGet("/{id}", async (string id, [FromQuery] float? pitch, HttpContext context) =>
        {
            if (pitch.HasValue)
            {
                string[] table =
                [
                    "0.5",
                    "0.53125",
                    "0.5625",
                    "0.59375",
                    "0.625",
                    "0.65625",
                    "0.6875",
                    "0.71875",
                    "0.75",
                    "0.78125",
                    "0.8125",
                    "0.84375",
                    "0.875",
                    "0.90625",
                    "0.9375",
                    "0.96875",
                    "1",
                    "1.03125",
                    "1.0625",
                    "1.09375",
                    "1.125",
                    "1.15625",
                    "1.1875",
                    "1.21875",
                    "1.25",
                    "1.28125",
                    "1.3125",
                    "1.34375",
                    "1.375",
                    "1.40625",
                    "1.4375",
                    "1.46875"
                ];
                if (!table.Contains(pitch.ToString()))
                {
                    return Results.BadRequest();
                }

                HttpClient client = new HttpClient();
                var res = await client.GetByteArrayAsync("http://localhost:8080/" + id + "?pitch=" + pitch);
                var resFinal = Results.File(res, "application/octet-stream", "处理完成.wav");
                client.Dispose();
                
                return resFinal;
            }

            if (cache.Any(i => i.Uuid == id))
            {
                var res = Results.File(cache.First(i => i.Uuid == id).File, "audio/mp3", enableRangeProcessing: true);
                context.Response.Headers["Content-Disposition"] = "attachment; filename=\"Music.mp3\"";
                
                return res;
            }

            var dbContext = new InstrunetDbContext();
            if (dbContext.InstrunetEntries.Any(i => i.Uuid == id))
            {
                var entry = dbContext.InstrunetEntries.First(i => i.Uuid == id)!;
                cache.Add(entry);
                
                return Results.File(entry.Databinary!, "audio/mp3", enableRangeProcessing: true);
            }

            await dbContext.DisposeAsync();
            
            return Results.NotFound();
        });
        return app;
    }

    public static WebApplication GetAlbumCover(this WebApplication app, List<QueueContext> cache)
    {
        app.MapGet("/getalbumcover", (string id) =>
        {
            if (cache.Any(i => i.Uuid == id))
            {
                var cover = cache.First(i => i.Uuid == id).AlbumCover;
                if (cover is null)
                {
                    return Results.BadRequest();
                }

                return Results.File(cover, "image/webp", enableRangeProcessing: true);
            }

            using var context = new InstrunetDbContext();
            if (!context.InstrunetEntries.Any(i => i.Uuid == id))
            {
                return Results.BadRequest();
            }

            var coverB = context.InstrunetEntries.First(i => i.Uuid == id).Albumcover;
            if (coverB is null)
            {
                return Results.BadRequest();
            }

            return Results.File(coverB, "image/webp", enableRangeProcessing: true);
        });
        return app;
    }

    // TODO the function of getting albumCover through this endpoint will be deprecated soon. 
    [Obsolete("Deprecated: // TODO the function of getting albumCover through this endpoint will be deprecated soon. ")]
    public static WebApplication GetSingleMetadata(this WebApplication app, List<QueueContext> cache)
    {
        app.MapGet("/getsingle", (string id, bool? albumCover) =>
        {
            using var context = new InstrunetDbContext();
            if (cache.Any(i => i.Uuid == id))
            {
                if (albumCover.HasValue && albumCover.Value)
                {
                    var arrCache = context.InstrunetEntries.Where(i => i.Uuid == id).Select(i => i.Albumcover).First();
                    var intArrayCache = arrCache?.Select(b => (int)b).ToArray();
                    
                    return Results.Json(new
                        { albumcover = arrCache == null ? null : new { type = "Buffer", data = intArrayCache } });
                }

                
                return Results.Json(cache.Where(i => i.Uuid == id).Select(i => new
                    { song_name = i.Name, album_name = i.AlbumName, artist = i.Artist, kind = i.Kind }).First());
            }

            if (context.InstrunetEntries.Any(i => i.Uuid == id))
            {
                if (albumCover.HasValue && albumCover.Value)
                {
                    var arr = context.InstrunetEntries.Where(i => i.Uuid == id).Select(i => i.Albumcover).First();

                    var intArray = arr?.Select(b => (int)b).ToArray();
                    
                    return Results.Json(new
                        { albumcover = arr == null ? null : new { type = "Buffer", data = intArray } });
                }

                
                return Results.Json(context.InstrunetEntries.Where(i => i.Uuid == id).Select(i => new
                    { song_name = i.SongName, album_name = i.AlbumName, artist = i.Artist, kind = i.Kind }).First());
            }

            
            return Results.NotFound();
        });
        return app;
    }

    public static WebApplication Search(this WebApplication app)
    {
        app.MapPost("/search_api", ([FromBody] SearchParamsPayload searchParams) =>
        {
            using var context = new InstrunetDbContext();
            if (searchParams.SearchStr != null)
            {
                searchParams.SearchStr = searchParams.SearchStr.Trim();
            }

            if (string.IsNullOrEmpty(searchParams.SearchStr))
            {
                var res = context.InstrunetEntries.Select(i => new
                {
                    uuid = i.Uuid, song_name = i.SongName, album_name = i.AlbumName, artist = i.Artist,
                    kind = i.Kind, Spell = i.SongName.ToPinyin()
                }).ToList();
                
                return Results.Json(res.OrderBy(x => x.Spell).ToList());
            }
            else
            {
                var res = context.InstrunetEntries.Where(i =>
                    (
                        i.SongName.Contains(searchParams.SearchStr) ||
                        i.AlbumName.Contains(searchParams.SearchStr) ||
                        i.Artist!.Contains(searchParams.SearchStr) ||
                        i.SongName.Contains(ChineseConverter.Convert(searchParams.SearchStr,
                            ChineseConversionDirection.SimplifiedToTraditional)) ||
                        i.AlbumName.Contains(ChineseConverter.Convert(searchParams.SearchStr,
                            ChineseConversionDirection.SimplifiedToTraditional)) ||
                        i.Artist!.Contains(ChineseConverter.Convert(searchParams.SearchStr,
                            ChineseConversionDirection.SimplifiedToTraditional))) ||
                    i.SongName.Contains(ChineseConverter.Convert(searchParams.SearchStr,
                        ChineseConversionDirection.SimplifiedToTraditional)) ||
                    i.AlbumName.Contains(ChineseConverter.Convert(searchParams.SearchStr,
                        ChineseConversionDirection.SimplifiedToTraditional)) ||
                    i.Artist!.Contains(ChineseConverter.Convert(searchParams.SearchStr,
                        ChineseConversionDirection.SimplifiedToTraditional))
                ).Select(i => new
                {
                    uuid = i.Uuid, song_name = i.SongName, album_name = i.AlbumName, artist = i.Artist,
                    kind = i.Kind, Spell = i.SongName.ToPinyin()
                }).ToList();
                
                return Results.Json(res.OrderBy(x => x.Spell).ToList());
            }
        });
        return app;
    }

    public static WebApplication Lyric(this WebApplication app)
    {
        app.MapGet("/lyric", async ([FromBody] LyricReceiveContextPayload lyricReceiveContext) =>
        {
            var client = new HttpClient();
            var resp = await client.GetAsync(
                $"http://andyxie.cn:28883/jsonapi?title={ChineseConverter.Convert(lyricReceiveContext.Name, ChineseConversionDirection.TraditionalToSimplified)}&artist={ChineseConverter.Convert(lyricReceiveContext.Artist, ChineseConversionDirection.TraditionalToSimplified)}&album={ChineseConverter.Convert(lyricReceiveContext.AlbumName, ChineseConversionDirection.TraditionalToSimplified)}");
            client.Dispose();
            return Results.Json(await resp.Content.ReadAsStringAsync());
        });
        return app;
    }

    public static WebApplication MapAllGetterEndpoints(this WebApplication app, List<QueueContext> cache)
    {
        var methods = typeof(MapGetterEndpoints).GetMethods(BindingFlags.Static | BindingFlags.Public);
        foreach (var method in methods)
        {
            switch (method.Name)
            {
                case "MapAllGetterEndpoints":
                    continue;
                case "Lyric":
                case "Search":
                    method.Invoke(null, [app]);
                    continue;
                default:
                    method.Invoke(null, [app, cache]);
                    continue;
            }
        }

        return app;
    }
}