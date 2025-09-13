using System.Reflection;
using InstrunetBackend.Server.Context;
using InstrunetBackend.Server.IndependantModels.HttpPayload;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InstrunetBackend.Server.Endpoints;

public static class MapUserEndpoints
{
    public static WebApplication UserApi(this WebApplication app)
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
                return Results.Ok(res);
            }

            if (string.IsNullOrWhiteSpace(uuidSession))
            {
                return Results.Unauthorized();
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

    public static WebApplication GetUploaded(this WebApplication app)
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
                    i.Uuid
                }).ToList());
        });
        return app;
    }

    public static WebApplication DeleteAccount(this WebApplication app)
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

    public static WebApplication LogOut(this WebApplication app)
    {
        app.MapGet("/logout", (HttpContext httpContext) =>
        {
            httpContext.Session.Clear();
            return Results.Ok();
        });
        return app;
    }

    public static WebApplication LogIn(this WebApplication app)
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

    public static WebApplication UploadAvatar(this WebApplication app)
    {
        app.MapPost("/uploadAvatar", (HttpContext context, [FromBody] AvatarUploadContextPayload form) =>
        {
            string? uuidSession = context.Session.GetString("uuid");
            if (string.IsNullOrWhiteSpace(uuidSession))
            {
                return Results.BadRequest();
            }

            byte[] byteArray = form.Avatar.SelectMany(BitConverter.GetBytes).ToArray();
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

    public static WebApplication GetAvatar(this WebApplication app)
    {
        app.MapGet("/avatar", (string uuid) =>
        {
            using var context = new InstrunetDbContext();
            var arr = context.Users.Where(i => i.Uuid == uuid).Select(i => i.Avatar).First();
            if (arr == null)
            {
                return Results.NotFound();
            }

            return Results.File(arr, "image/png");
        });
        return app;
    }

    public static WebApplication DeleteOwnSong(this WebApplication app)
    {
        app.MapGet("/delSong", (string uuid, HttpContext context) =>
        {
            var sessionUuid = context.Session.GetString("uuid");
            if (string.IsNullOrEmpty(sessionUuid))
            {
                return Results.BadRequest();
            }

            using var dbContext = new InstrunetDbContext();
            var entry = dbContext.InstrunetEntries.FirstOrDefault(i => i.Uuid == uuid);
            if (entry is null)
            {
                return new NotFoundResult();
            }

            if (entry.User == sessionUuid)
            {
                dbContext.InstrunetEntries.Where(i => i.Uuid == uuid).ExecuteDelete();
                return new OkResult();
            }

            return BadRequest();
        });
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