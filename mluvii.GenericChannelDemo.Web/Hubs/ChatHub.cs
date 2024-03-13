using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using mluvii.GenericChannelDemo.Web.Models;

namespace mluvii.GenericChannelDemo.Web.Hubs
{
    public class ChatHub : Hub
    {
        public async Task Send(string message)
        {
            var model = new MessageModel
            {
                Timestamp = DateTimeOffset.Now,
                IsIncoming = false,
                Content = message
            };

            await Clients.Caller.SendAsync("AddMessages", new[] { model });
        }
    }
}
