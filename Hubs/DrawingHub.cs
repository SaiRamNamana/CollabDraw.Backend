namespace CollabDraw.Backend.Hubs;

using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using CollabDraw.Backend.Data;
using CollabDraw.Backend.Models;

public class DrawingHub : Hub
{
    private sealed record RoomUser(string ConnectionId, string UserName);
    private sealed record RemoteCursor(string ConnectionId, double X, double Y, string UserName, bool IsDrawing);
    private readonly AppDbContext _db;
    private static readonly ConcurrentDictionary<string, (string RoomId, string UserName)> Connections = new();

    public DrawingHub(AppDbContext db) => _db = db;

    public async Task JoinRoom(string roomId, string userName)
    {
        if (string.IsNullOrWhiteSpace(roomId))
            throw new HubException("Room ID is required.");

        if (string.IsNullOrWhiteSpace(userName))
            throw new HubException("User name is required.");

        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        Connections[Context.ConnectionId] = (roomId, userName);

        var updates = await _db.RoomUpdates
            .Where(u => u.RoomId == roomId)
            .OrderBy(u => u.CreatedAt)
            .ThenBy(u => u.Id)
            .Select(u => u.UpdateData)
            .ToListAsync();

        foreach (var update in updates)
            await Clients.Caller.SendAsync("ReceiveUpdate", update);

        var activeUsers = GetRoomUsers(roomId);

        await Clients.Group(roomId).SendAsync("PresenceUpdate", activeUsers);
    }

    public async Task LeaveRoom(string roomId)
    {
        if (Connections.TryRemove(Context.ConnectionId, out var info))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
            var activeUsers = GetRoomUsers(info.RoomId);
            await Clients.Group(info.RoomId).SendAsync("PresenceUpdate", activeUsers);
        }
    }

    public async Task ResetRoom(string roomId)
    {
        var updates = await _db.RoomUpdates
            .Where(update => update.RoomId == roomId)
            .ToListAsync();

        _db.RoomUpdates.RemoveRange(updates);
        await _db.SaveChangesAsync();
        await Clients.Group(roomId).SendAsync("RoomReset");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Connections.TryRemove(Context.ConnectionId, out var info))
        {
            var activeUsers = GetRoomUsers(info.RoomId);
            await Clients.Group(info.RoomId).SendAsync("PresenceUpdate", activeUsers);
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendUpdate(string roomId, byte[] update)
    {
        await Clients.OthersInGroup(roomId).SendAsync("ReceiveUpdate", update);

        _db.RoomUpdates.Add(new RoomUpdate { RoomId = roomId, UpdateData = update });
        await _db.SaveChangesAsync();
    }

    public async Task SendCursor(string roomId, double x, double y, bool isDrawing)
    {
        var userName = Connections.TryGetValue(Context.ConnectionId, out var info) ? info.UserName : "Anonymous";
        await Clients.OthersInGroup(roomId).SendAsync(
            "ReceiveCursor",
            new RemoteCursor(Context.ConnectionId, x, y, userName, isDrawing));
    }

    private static List<RoomUser> GetRoomUsers(string roomId) => Connections
        .Where(connection => connection.Value.RoomId == roomId)
        .Select(connection => new RoomUser(connection.Key, connection.Value.UserName))
        .ToList();
}