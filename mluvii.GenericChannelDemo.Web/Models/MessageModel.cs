using System;

namespace mluvii.GenericChannelDemo.Web.Models
{
    public class MessageModel
    {
        public DateTimeOffset Timestamp { get; set; }

        public bool IsIncoming { get; set; }

        public string Content { get; set; }
    }
}
