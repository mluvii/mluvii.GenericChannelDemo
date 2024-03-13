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
using StackExchange.Redis;

namespace mluvii.GenericChannelDemo.Web.Services.Impl
{
    public class ChatService : IChatService
    {
        private readonly IHttpClientFactory httpClientFactory;

        private readonly ConnectionMultiplexer redis;
        private readonly IDatabase database;

        public ChatService(IHttpClientFactory httpClientFactory, IOptions<RedisOptions> redisOptions)
        {
            this.httpClientFactory = httpClientFactory;

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
            await SendToWebhook(conversationId, model);
            await database.ListRightPushAsync(GetKey(conversationId), JsonConvert.SerializeObject(model));
        }

        public async Task<string> ReceiveMessage(string conversationId, MessageModel model)
        {
            await database.ListRightPushAsync(GetKey(conversationId), JsonConvert.SerializeObject(model));
            return Guid.NewGuid().ToString("N");
        }

        private record WebhookInfo(string Url, IDictionary<string, string> Headers);

        public Task RegisterWebhook(string webhookUrl, IDictionary<string, string> webhookHeaders)
        {
            return database.StringSetAsync("webhook", JsonConvert.SerializeObject(new WebhookInfo(webhookUrl, webhookHeaders)));
        }

        private async Task SendToWebhook(string conversationId, MessageModel model)
        {
            var (webhookUrl, webhookHeaders) = JsonConvert.DeserializeObject<WebhookInfo>(await database.StringGetAsync("webhook"));
            using var httpClient = httpClientFactory.CreateClient();
            foreach (var webhookHeader in webhookHeaders)
            {
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation(webhookHeader.Key, webhookHeader.Value);
            }

            var payloadString = JsonConvert.SerializeObject(new GenericChannelWebhookPayload
            {
                Activities = new []
                {
                    new GenericChannelIncomingActivity
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        Timestamp = model.Timestamp,
                        ConversationId = conversationId,
                        Type = GenericChannelActivityType.ChatMessage,
                        Text = model.Content
                    }
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
