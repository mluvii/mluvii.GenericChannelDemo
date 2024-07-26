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

        public class ReceivedMessageNotifier : IChatReceivedMessageNotifier
        {
            private readonly IHubContext<ChatHub> hubContext;

            public ReceivedMessageNotifier(IHubContext<ChatHub> hubContext)
            {
                this.hubContext = hubContext;
            }

            public Task Notify(string conversationId, MessageModel model)
            {
                return hubContext.Clients.User(conversationId).SendAsync("AddMessages", new[] { model });
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

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await chatService.LeaveConversation(Context.UserIdentifier);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task Send(string message)
        {
            var model = new MessageModel
            {
                Timestamp = DateTimeOffset.Now,
                MessageType = MessageType.Sent,
                Content = message
            };

            await chatService.SendMessage(Context.UserIdentifier, model);

            await Clients.Caller.SendAsync("AddMessages", new[] { model });
        }
    }
}
