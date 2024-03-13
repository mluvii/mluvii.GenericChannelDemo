using System.Collections.Generic;
using System.Threading.Tasks;
using mluvii.GenericChannelDemo.Web.Models;

namespace mluvii.GenericChannelDemo.Web.Services
{
    public interface IChatService
    {
        Task<MessageModel[]> GetMessages(string conversationId);

        Task SendMessage(string conversationId, MessageModel model);

        Task<string> ReceiveMessage(string conversationId, MessageModel model);

        Task RegisterWebhook(string webhookUrl, IDictionary<string,string> webhookHeaders);
    }
}
