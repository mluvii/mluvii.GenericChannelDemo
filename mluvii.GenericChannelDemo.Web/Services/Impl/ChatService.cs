using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using mluvii.GenericChannelDemo.Web.Models;
using mluvii.GenericChannelModels.Activity;
using mluvii.GenericChannelModels.Webhook;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using StackExchange.Redis;

namespace mluvii.GenericChannelDemo.Web.Services.Impl
{
    public class ChatService : IChatService
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IList<IChatReceivedMessageNotifier> receivedMessageNotifiers;

        private readonly ConnectionMultiplexer redis;
        private readonly IDatabase database;

        public ChatService(
            IHttpClientFactory httpClientFactory,
            IEnumerable<IChatReceivedMessageNotifier> receivedMessageNotifiers,
            IOptions<RedisOptions> redisOptions)
        {
            this.httpClientFactory = httpClientFactory;
            this.receivedMessageNotifiers = receivedMessageNotifiers.ToList();

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
            await SendToWebhook(new GenericChannelIncomingActivity
            {
                Id = Guid.NewGuid().ToString("N"),
                Timestamp = model.Timestamp,
                ConversationId = conversationId,
                Type = GenericChannelActivityType.ChatMessage,
                Text = model.Content
            });

            await database.ListRightPushAsync(GetKey(conversationId), JsonConvert.SerializeObject(model));
        }

        public async Task LeaveConversation(string conversationId)
        {
            await SendToWebhook(new GenericChannelIncomingActivity
            {
                Id = Guid.NewGuid().ToString("N"),
                Timestamp = DateTimeOffset.Now,
                ConversationId = conversationId,
                Type = GenericChannelActivityType.GuestLeft
            });
        }

        public async Task<string> ReceiveMessage(string conversationId, MessageModel model)
        {
            await database.ListRightPushAsync(GetKey(conversationId), JsonConvert.SerializeObject(model));

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(async () =>
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            {
                foreach (var notifier in receivedMessageNotifiers)
                {
                    await notifier.Notify(conversationId, model);
                }
            });

            return Guid.NewGuid().ToString("N");
        }

        private record WebhookInfo(string Url, IDictionary<string, string> Headers);

        public Task RegisterWebhook(string webhookUrl, IDictionary<string, string> webhookHeaders)
        {
            return database.StringSetAsync("webhook", JsonConvert.SerializeObject(new WebhookInfo(webhookUrl, webhookHeaders)));
        }

        private async Task SendToWebhook(GenericChannelIncomingActivity activity)
        {
            var (webhookUrl, webhookHeaders) = JsonConvert.DeserializeObject<WebhookInfo>(await database.StringGetAsync("webhook"));
            using var httpClient = httpClientFactory.CreateClient();
            foreach (var webhookHeader in webhookHeaders)
            {
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation(webhookHeader.Key, webhookHeader.Value);
            }

            var payload = new GenericChannelWebhookPayload
            {
                Activities = new[] { activity }
            };

            var payloadString = JsonConvert.SerializeObject(payload, Formatting.None, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = new List<JsonConverter>
                {
                    new StringEnumConverter()
                }
            });

            using var content = new StringContent(payloadString, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, webhookUrl);
            request.Content = content;

            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
    }
}
