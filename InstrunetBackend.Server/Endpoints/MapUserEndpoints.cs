using InstrunetBackend.Server.Context;
using InstrunetBackend.Server.IndependantModels.HttpPayload;
using InstrunetBackend.Server.InstrunetModels;
using InstrunetBackend.Server.lib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.Reflection;

namespace InstrunetBackend.Server.Endpoints;

public static class MapUserEndpoints
{
    public static WebApplication MapUserApi(this WebApplication app)
    {
        app.MapGet("/userapi", (HttpContext context, string? uuid = null, bool getName = false) =>
        {
            using var dbContext = new InstrunetDbContext();
            var uuidSession = context.Session.GetString("uuid");
            if (getName)
            {
                if (string.IsNullOrWhiteSpace(uuid))
                {
                    return Results.BadRequest();
                }

                if (!dbContext.Users.Any(i => i.Uuid == uuid))
                {
                    return Results.NotFound();
                }

                var res = dbContext.Users.Where(i => i.Uuid == uuid).Select(i => i.Username).First();
                return Results.Text(res);
            }

            if (string.IsNullOrWhiteSpace(uuidSession))
            {
                return Results.StatusCode(500);
            }

            if (!dbContext.Users.Any(i => i.Uuid == uuidSession))
            {
                return Results.NotFound();
            }

            var api = dbContext.Users.Where(i => i.Uuid == uuidSession)
                .Select(i => new { uuid = uuidSession, username = i.Username, email = i.Email }).First();
            return Results.Json(api);
        });
        return app;
    }

    public static WebApplication MapGetUploaded(this WebApplication app)
    {
        app.MapGet("/getUploaded", (HttpContext context) =>
        {
            if (string.IsNullOrWhiteSpace(context.Session.GetString("uuid")))
            {
                return Results.BadRequest();
            }

            using var dbContext = new InstrunetDbContext();
            return Results.Json(dbContext.InstrunetEntries.Where(i => i.User == context.Session.GetString("uuid"))
                .Select(i => new
                {
                    i.Uuid, i.Epoch
                }).ToList().OrderByDescending(i => i.Epoch.ToString().Length == 10 ? DateTimeOffset.FromUnixTimeSeconds(i.Epoch) :  DateTimeOffset.FromUnixTimeMilliseconds(i.Epoch)));
        });
        return app;
    }

    public static WebApplication MapDeleteAccount(this WebApplication app)
    {
        app.MapDelete("/delAcc", (HttpContext context) =>
        {
            if (context.Session.GetString("uuid") == null)
            {
                return Results.BadRequest();
            }

            using var dbContext = new InstrunetDbContext();
            var res = dbContext.Users.Where(i => i.Uuid == context.Session.GetString("uuid")).ExecuteDelete();
            if (res == 0)
            {
                return Results.InternalServerError();
            }

            return Results.Ok();
        });
        return app;
    }

    public static WebApplication MapLogOut(this WebApplication app)
    {
        app.MapGet("/logout", (HttpContext httpContext) =>
        {
            httpContext.Session.Clear();
            return Results.Ok();
        });
        return app;
    }

    public static WebApplication MapLogIn(this WebApplication app)
    {
        app.MapPost("/login", ([FromBody] UserFormPayload form, HttpContext httpContext) =>
        {
            using var context = new InstrunetDbContext();
            var userLogin = context.Users.FirstOrDefault(i =>
                (i.Username == form.Username && i.Password == form.Password.Sha256HexHashString()) ||
                (i.Email == form.Username && i.Password == (form.Password).Sha256HexHashString()));
            if (userLogin != null)
            {
                httpContext.Session.SetString("uuid", userLogin.Uuid);
                return Results.Json(new
                {
                    uid = userLogin.Uuid,
                });
            }

            return Results.NotFound();
        });
        return app;
    }

    public static WebApplication MapUploadAvatar(this WebApplication app)
    {
        app.MapPost("/uploadAvatar", async (HttpContext context) =>
        {
            string? uuidSession = context.Session.GetString("uuid");
            if (string.IsNullOrWhiteSpace(uuidSession))
            {
                return Results.BadRequest();
            }

            var data = await new StreamReader(context.Request.Body).ReadLineAsync();
            if (data is null)
            {
                return Results.BadRequest(); 
            }

            byte[] byteArray = data.DataUrlToByteArray();
            var builder = LibraryHelper.CreateWebPEncoderBuilder();
            try
            {
                if (builder is not null)
                {
                    var encoder = builder.CompressionConfig(x => x.Lossy(y => y.Quality(80).Size(100000))).Build();
                    using var input = new MemoryStream(byteArray);
                    using var output = new MemoryStream();
                    encoder.Encode(input, output);
                    byteArray = output.ToArray();
                }
            }
            catch (Exception e)
            {
                return Results.BadRequest("文件不支持或不合法: " + e);
            }
            
            using var dbContext = new InstrunetDbContext();
            var rows = dbContext.Users.Where(i => i.Uuid == uuidSession)
                .ExecuteUpdate(setter => setter.SetProperty(i => i.Avatar, byteArray));
            if (rows == 0)
            {
                return Results.NotFound();
            }

            return Results.Ok();
        });
        return app;
    }

    public static WebApplication MapGetAvatar(this WebApplication app)
    {
        app.MapGet("/avatar", (string uuid) =>
        {
            using var context = new InstrunetDbContext();
            var arr = context.Users.Where(i => i.Uuid == uuid).Select(i => i.Avatar).FirstOrDefault();
            if (arr == null)
            {
                return Results.NotFound();
            }

            return Results.File(arr, "image/png");
        });
        return app;
    }

    public static WebApplication MapDeleteOwnSong(this WebApplication app)
    {
        app.MapGet("/delSong", (string uuid, HttpContext context) =>
        {
            var sessionUuid = context.Session.GetString("uuid");
            if (string.IsNullOrWhiteSpace(sessionUuid))
            {
                return Results.BadRequest();
            }

            using var dbContext = new InstrunetDbContext();
            var entry = dbContext.InstrunetEntries.FirstOrDefault(i => i.Uuid == uuid);
            if (entry is null)
            {
                return Results.NotFound();
            }

            if (entry.User == sessionUuid)
            {
                dbContext.InstrunetEntries.Where(i => i.Uuid == uuid).ExecuteDelete();
                return Results.Ok();
            }

            return Results.BadRequest();
        });
        return app;
    }
    public static WebApplication MapRegister(this WebApplication app)
    {
        app.MapPost("/register", ([FromBody] RegisterContextPayload payload, HttpContext httpContext) =>
        {
            using var dbContext = new InstrunetDbContext();
            if(dbContext.Users.Any(i=>i.Username == payload.Username.Trim()))
            {
                return Results.BadRequest(); 
            }

            var newUuid = Guid.NewGuid().ToString(); 
            dbContext.Users.Add(new User
            {
                Uuid = newUuid,
                Username = payload.Username.Trim(),
                Password = payload.Password.Sha256HexHashString(),
                Email = payload.Email,
                Time = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds()
            });
            dbContext.SaveChanges();
                httpContext.Session.SetString("uuid", newUuid);
            
            return Results.Json(new
            {
                uid = newUuid
            });
        }); 
        return app;
    }

    public static WebApplication MapAllUserEndpoints(this WebApplication app)
    {
        var methods = typeof(MapUserEndpoints).GetMethods(BindingFlags.Static | BindingFlags.Public);
        foreach (var methodInfo in methods)
        {
            switch (methodInfo.Name)
            {
                case "MapAllUserEndpoints":
                    continue;
                default:
                    methodInfo.Invoke(null, [app]);
                    continue;
            }
        }

        return app;
    }
}