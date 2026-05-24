using Microsoft.AspNetCore.SignalR;

namespace RaceTimerServer.Hubs;

public class RaceHub : Hub
{
    // simple hub - clients subscribe to updates for races
    public async Task SubscribeRace(string raceId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(raceId));
    }

    public async Task UnsubscribeRace(string raceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(raceId));
    }

    public static string GetGroupName(string raceId) => $"race_{raceId}";
}
