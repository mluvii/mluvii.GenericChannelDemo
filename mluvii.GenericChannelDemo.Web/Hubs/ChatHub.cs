using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using mluvii.GenericChannelDemo.Web.Models;
using mluvii.GenericChannelDemo.Web.Services;

namespace mluvii.GenericChannelDemo.Web.Hubs
{
    public class ChatHub : Hub
    {
        public class UserIdProvider : IUserIdProvider
        {
            public string GetUserId(HubConnectionContext connection)
            {
                return connection.GetHttpContext()?.Request.Query["conversationId"];
            }
        }

        private readonly IChatService chatService;

        public ChatHub(IChatService chatService)
        {
            this.chatService = chatService;
        }

        public override async Task OnConnectedAsync()
        {
            var models = await chatService.GetMessages(Context.UserIdentifier);
            await Clients.Caller.SendAsync("AddMessages", models);
            await base.OnConnectedAsync();
        }

        public async Task Send(string message)
        {
            var model = new MessageModel
            {
                Timestamp = DateTimeOffset.Now,
                IsIncoming = false,
                Content = message
            };

            await chatService.SendMessage(Context.UserIdentifier, model);

            await Clients.Caller.SendAsync("AddMessages", new[] { model });
        }
    }
}
