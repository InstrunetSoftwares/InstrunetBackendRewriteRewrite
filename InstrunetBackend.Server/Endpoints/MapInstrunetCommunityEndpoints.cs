using System.Reflection;
using InstrunetBackend.Server.Context;
using InstrunetBackend.Server.IndependantModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InstrunetBackend.Server.Endpoints;

public static class MapInstrunetCommunityEndpoints
{
    public static RouteGroupBuilder Upvote(this RouteGroupBuilder app)
    {
        app.MapPost("/upvote", ([FromBody] string uuid, HttpContext httpContext) =>
        {
            var sessionUuid = httpContext.Session.GetString("uuid");
            if (string.IsNullOrWhiteSpace(sessionUuid))
            {
                return Results.Unauthorized(); 
            }

            using var context = new InstrunetDbContext(); 
            if (context.InstrunetEntries.Count(i => i.Uuid == uuid) == 0)
            {
                return Results.NotFound("查无此歌");
            }

            if (context.Votes.Any(i => i.Master == uuid && i.User == sessionUuid && i.IsUpvote))
            {
                return Results.BadRequest("Voted. ");
            }

            if (context.Votes.Any(i => i.Master == uuid && i.User == sessionUuid))
            {
                context.Votes.FirstOrDefault(i => i.Master == uuid && i.User == sessionUuid)!.IsUpvote = true;
                context.SaveChanges();
                return Results.Ok();
            }

            context.Votes.Add(new()
            {
                Uuid = Guid.NewGuid().ToString(), IsUpvote = true, Master = uuid, User = sessionUuid
            });
            context.SaveChanges();
            return Results.Ok();
        }); 
        return app; 
    }

    public static RouteGroupBuilder Downvote(this RouteGroupBuilder app)
    {
        app.MapPost("/downvote", ([FromBody] string uuid, HttpContext httpContext) =>
        {
            var sessionUuid = httpContext.Session.GetString("uuid");
            if (string.IsNullOrWhiteSpace(sessionUuid))
            {
                return Results.Unauthorized();
            }

            using var context = new InstrunetDbContext();
            if (context.InstrunetEntries.Count(i => i.Uuid == uuid) == 0)
            {
                return Results.NotFound("查无此歌");
            }

            if (context.Votes.Any(i => i.Master == uuid && i.User == sessionUuid && !i.IsUpvote))
            {
                return Results.BadRequest("Voted. ");
            }

            if (context.Votes.Any(i => i.Master == uuid && i.User == sessionUuid))
            {
                context.Votes.FirstOrDefault(i => i.Master == uuid && i.User == sessionUuid)!.IsUpvote = false;
                context.SaveChanges();
                return Results.Ok();
            }

            context.Votes.Add(new()
            {
                Uuid = Guid.NewGuid().ToString(), IsUpvote = false, Master = uuid, User = sessionUuid
            });
            context.SaveChanges();
            return Results.Ok();
        });
        return app; 
    }

    public static RouteGroupBuilder GetVote(this RouteGroupBuilder app)
    {
        app.MapGet("/getVote", (string uuid) =>
        {
            using var context = new InstrunetDbContext(); 
            if (context.Votes.Count(i => i.Master == uuid && i.IsUpvote) -
                context.Votes.Count(i => i.Master == uuid && !i.IsUpvote) <= -10)
            {
                context.InstrunetEntries.Where(i => i.Uuid == uuid).ExecuteDelete();
                context.Votes.Where(i => i.Master == uuid).ExecuteDelete();
                return Results.Ok(-10);
            }

            return Results.Ok(context.Votes.Count(i => i.Master == uuid && i.IsUpvote) -
                              context.Votes.Count(i => i.Master == uuid && !i.IsUpvote)); 
        });
        return app; 
    }

    public static RouteGroupBuilder PostComment(this RouteGroupBuilder app)
    {
        app.MapPost("/postComment", ([FromBody] PostCommentPayload postCommentPayload, HttpContext httpContext) =>
        {
            using var context = new InstrunetDbContext();
            var sessionUuid = httpContext.Session.GetString("uuid");
            if (string.IsNullOrEmpty(sessionUuid))
            {
                return Results.Unauthorized();
            }

            if (context.InstrunetEntries.Any(i => i.Uuid == postCommentPayload.Master)||context.Comments.Any(i => i.Uuid == postCommentPayload.Master))
            {
                try
                {
                    context.Comments.Add(new()
                    {
                        Content = postCommentPayload.Content,
                        Date = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), Master = postCommentPayload.Master,
                        Poster = sessionUuid,
                        Uuid = Guid.NewGuid().ToString()
                    });
                    context.SaveChanges(); 
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    return Results.InternalServerError(ex.Message);
                }
            }

            return Results.BadRequest("bruh. ");   
        });
        return app; 
    }

    public static RouteGroupBuilder GetComment(this RouteGroupBuilder app)
    {
        app.MapGet("/getComment", (string uuid) =>
        {
            using var context = new InstrunetDbContext();
            return Results.Json(context.Comments.Where(i => i.Master == uuid).ToList()); 
        });
        return app;
    }
    public static WebApplication MapAllInstrunetCommunityEndpoints(this WebApplication app)
    {
        var instrunetCommunityApis = app.MapGroup("/api/community"); 
        var methods = typeof(MapInstrunetCommunityEndpoints).GetMethods(BindingFlags.Static | BindingFlags.Public);
        foreach (var methodInfo in methods)
        {
            switch (methodInfo.Name)
            {
                case "MapAllInstrunetCommunityEndpoints":
                    continue;
                default:
                    methodInfo.Invoke(null, [instrunetCommunityApis]);
                    continue;
            }
        }
        return app; 
    }
}