using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace mluvii.GenericChannelDemo.Web.Models
{
    public class MessageModel
    {
        public DateTimeOffset Timestamp { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public MessageType MessageType { get; set; }

        public string Content { get; set; }
    }
}
