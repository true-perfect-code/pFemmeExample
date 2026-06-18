using Microsoft.AspNetCore.SignalR;

namespace pWebApi.Hubs
{
    public class AuthHub : Hub
    {
        public async Task SubscribeToAuth(string pollingId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, pollingId);
        }
    }
}
