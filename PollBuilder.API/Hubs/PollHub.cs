using Microsoft.AspNetCore.SignalR;

namespace PollBuilder.API.Hubs
{
    public class PollHub : Hub
    {
        // Mun's frontend will call this when a user opens a specific poll's results page
        public async Task JoinPollGroup(string code)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, code);
        }

        // Mun's frontend will call this when they leave the page
        public async Task LeavePollGroup(string code)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, code);
        }
    }
}