using InstrunetBackend.Server.Chatroom;
using InstrunetBackend.Server.Chatroom.Models;

namespace InstrunetBackend.Server.Endpoints;

public static class ChatroomEndpoints
{
    extension(IEndpointRouteBuilder app)
    {
        public IEndpointRouteBuilder MapChatroom()
        {
            app.MapGet("get", (HttpContext context, ChatroomDbContext db, int maxCount = 20) =>
            {
                return Results.Ok(db.Messages.OrderByDescending(o => o.Time).Take(maxCount).AsEnumerable());
            });
            app.MapGet("long-poll", async (HttpContext context, ChatroomDbContext db, CancellationToken ct) =>
            {
                var current = db.Messages.OrderByDescending(i => i.Time).FirstOrDefault();
                Message? toCheck = db.Messages.OrderByDescending(i => i.Time).FirstOrDefault();
                while (!ct.IsCancellationRequested &&
                       toCheck == current)
                {
                    await Task.Delay(750, ct);
                    db.ChangeTracker.Clear();
                    toCheck = db.Messages.OrderByDescending(i => i.Time).FirstOrDefault();
                }
                return toCheck;
            }).DisableRequestTimeout();
            app.MapPost("post", async (HttpContext context, ChatroomDbContext db, PostMessageBody body) =>
            {
                db.Messages.Add(body);
                await db.SaveChangesAsync();
                return Results.Ok();
            });
            return app; 
        }
        public IEndpointRouteBuilder MapAllChatroomEndpoints()
        {
            app.MapGroup("/api/v1/chatroom").MapChatroom();
            return app; 
        }
    }
}