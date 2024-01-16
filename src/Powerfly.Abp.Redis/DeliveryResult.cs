using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Powerfly.Abp.Redis
{
    public class DeliveryResult<TKey, TValue>
    {
        public string TopicName { get; }
        public string Key { get; }
        public Message<TKey, TValue> Message { get; set; }

        public DeliveryResult(string topicName, string key, Message<TKey, TValue> message)
        {
            TopicName = topicName;
            Key = key;
            Message = message;
        }
    }
}
