using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using mluvii.GenericChannelDemo.Web.Models;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace mluvii.GenericChannelDemo.Web.Services.Impl
{
    public class ChatService : IChatService
    {
        private readonly ConnectionMultiplexer redis;
        private readonly IDatabase database;

        public ChatService(IOptions<RedisOptions> redisOptions)
        {
            redis = ConnectionMultiplexer.Connect(redisOptions.Value.Url);
            database = redis.GetDatabase(redisOptions.Value.Db);
        }

        private static string GetKey(string conversationId) => "conv_" + conversationId;

        public async Task<MessageModel[]> GetMessages(string conversationId)
        {
            var stored = await database.ListRangeAsync(GetKey(conversationId));
            return stored.Select(v => JsonConvert.DeserializeObject<MessageModel>(v)).ToArray();
        }

        public async Task SendMessage(string conversationId, MessageModel model)
        {
            await database.ListRightPushAsync(GetKey(conversationId), JsonConvert.SerializeObject(model));
        }
    }
}
