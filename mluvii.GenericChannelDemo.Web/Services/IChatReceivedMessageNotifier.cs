using System.Threading.Tasks;
using mluvii.GenericChannelDemo.Web.Models;

namespace mluvii.GenericChannelDemo.Web.Services
{
    public interface IChatReceivedMessageNotifier
    {
        Task Notify(string conversationId, MessageModel model);
    }
}
