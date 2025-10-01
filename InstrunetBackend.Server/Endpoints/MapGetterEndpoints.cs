using InstrunetBackend.Server.Context;
using InstrunetBackend.Server.IndependantModels;
using InstrunetBackend.Server.IndependantModels.HttpPayload;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.International.Converters.TraditionalChineseToSimplifiedConverter;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Net;
using System.Reflection;
using System.Text;
using InstrunetBackend.Server.lib;

namespace InstrunetBackend.Server.Endpoints;

public static class MapGetterEndpoints
{
    public static WebApplication SongAndPitching(this WebApplication app, List<QueueContext> cache)
    {
        app.MapGet("/{id}", async (string id, [FromQuery] float? pitch, HttpContext context) =>
        {
            await using var dbContext = new InstrunetDbContext();
            
            if (pitch.HasValue)
            {
                var data = dbContext.InstrunetEntries.FirstOrDefault(i => i.Uuid == id)?.Databinary;
                if (data is null)
                {
                    return Results.NotFound(); 
                }
                using var memStream = data.ToPitched((double)pitch);
                context.Response.Headers["Content-Disposition"] = "attachment; filename=\"Processed.mp3\"";
                var dataProcessed = memStream.ToArray();
                return Results.File(dataProcessed, "audio/mp3", enableRangeProcessing: true); 
            }

            if (cache.Any(i => i.Uuid == id))
            {
                var res = Results.File(cache.First(i => i.Uuid == id).File, "audio/mp3", enableRangeProcessing: true);
                context.Response.Headers["Content-Disposition"] = "attachment; filename=\"Music.mp3\"";

                return res;
            }

            if (dbContext.InstrunetEntries.Any(i => i.Uuid == id))
            {
                var entry = dbContext.InstrunetEntries.First(i => i.Uuid == id)!;
                cache.Add(entry);
                context.Response.Headers["Content-Disposition"] = "attachment; filename=\"Music.mp3\"";

                return Results.File(entry.Databinary!, "audio/mp3", enableRangeProcessing: true);
            }


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

            var coverB = context.InstrunetEntries.Where(i => i.Uuid == id).Select(i => i.Albumcover).First();
            if (coverB is null)
            {
                return Results.BadRequest();
            }

            return Results.File(coverB, "image/webp", enableRangeProcessing: true);
        });
        return app;
    }

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
                    uuid = i.Uuid,
                    song_name = i.SongName,
                    album_name = i.AlbumName,
                    artist = i.Artist,
                    kind = i.Kind,
                    Spell = i.SongName.ToPinyin()
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
                    uuid = i.Uuid,
                    song_name = i.SongName,
                    album_name = i.AlbumName,
                    artist = i.Artist,
                    kind = i.Kind,
                    Spell = i.SongName.ToPinyin()
                }).ToList();

                return Results.Json(res.OrderBy(x => x.Spell).ToList());
            }
        });
        return app;
    }

    public static WebApplication Lyric(this WebApplication app)
    {
        app.MapPost("/lyric", async ([FromBody] LyricReceiveContextPayload lyricReceiveContext) =>
        {
            var client = new HttpClient();
            var resp = await client.GetAsync(
                $"http://andyxie.cn:28883/jsonapi?title={ChineseConverter.Convert(lyricReceiveContext.Name, ChineseConversionDirection.TraditionalToSimplified)}&artist={ChineseConverter.Convert(lyricReceiveContext.Artist, ChineseConversionDirection.TraditionalToSimplified)}&album={ChineseConverter.Convert(lyricReceiveContext.AlbumName, ChineseConversionDirection.TraditionalToSimplified)}");
            client.Dispose();
            return Results.Json(await resp.Content.ReadFromJsonAsync<dynamic>());
        });
        return app;
    }
    public static WebApplication DownloadNeteaseMusic(this WebApplication app)
    {
        app.MapGroup("/api/ncmstuff").MapGet("downloadmusic", async (string id, HttpContext httpContext) =>
        {
            try
            {
                using var http = new HttpClient();
                using var handler = new HttpClientHandler();
                handler.UseCookies = false;
                using var client = new HttpClient(handler);
                client.BaseAddress = new Uri("http://localhost:3958");
                using var message = new HttpRequestMessage(HttpMethod.Get,
                    "/song/download/url/v1?id=" + id + "&level=hires");
                var stream = Assembly.GetExecutingAssembly()
                        .GetManifestResourceStream("InstrunetBackend.Server.NcmSecret");
                var memStream = new MemoryStream();
                await stream!.CopyToAsync(memStream);
                await stream.DisposeAsync();
                var secret = Encoding.UTF8.GetString(memStream.ToArray());
                await memStream.DisposeAsync();
                message.Headers.Add("Cookie", secret);
                var result = await client.SendAsync(message);
                if (result.StatusCode is HttpStatusCode.OK or HttpStatusCode.NotModified)
                {
                    dynamic? end = JsonConvert.DeserializeObject<dynamic>(await result.Content.ReadAsStringAsync());
                    if (end?.data.url != null)
                    {
                        dynamic info = JsonConvert.DeserializeObject(
                            await http.GetStringAsync(
                                "http://localhost:3958/song/detail?ids=" + id))!;
                        var file = Results.File(await http.GetByteArrayAsync((string)end.data.url), "audio/flac", enableRangeProcessing: true);
                        httpContext.Response.Headers["Content-Disposition"] = $"attachment; filename=\"target.flac\"";
                        return file;
                    }
                }
                return Results.BadRequest("不存在");

            }
            catch (Exception ex)
            {
                return Results.BadRequest(ex.Message);
            }
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
                case "DownloadNeteaseMusic":
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