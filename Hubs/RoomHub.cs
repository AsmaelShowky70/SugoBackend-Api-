using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace SugoBackend.Hubs;

[Authorize]
public class RoomHub : Hub
{
    public async Task JoinRoom(string roomId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
    }

    public async Task LeaveRoom(string roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
    }

    public async Task SendMessage(string roomId, string message)
    {
        var userId = GetCurrentUserId();
        await Clients.Group(roomId).SendAsync("ReceiveMessage", new
        {
            roomId,
            userId,
            message,
            sentAt = DateTime.UtcNow
        });
    }

    public async Task NotifyGift(string roomId, int giftId, int quantity)
    {
        var userId = GetCurrentUserId();
        await Clients.Group(roomId).SendAsync("GiftReceived", new
        {
            roomId,
            giftId,
            quantity,
            senderUserId = userId,
            createdAt = DateTime.UtcNow
        });
    }

    private int? GetCurrentUserId()
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null || !int.TryParse(claim.Value, out var id))
        {
            return null;
        }

        return id;
    }
}
