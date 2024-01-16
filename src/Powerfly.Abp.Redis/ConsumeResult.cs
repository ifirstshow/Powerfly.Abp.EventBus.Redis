using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Powerfly.Abp.Redis
{
    public class ConsumeResult<TKey, TValue>
    {
        public static ConsumeResult<TKey, TValue> EOF = new ConsumeResult<TKey, TValue>(default!, default!)
        {
            IsPartitionEOF = true,
        };

        public string TopicName { get; }
        public Message<TKey, TValue> Message { get; }
        public bool IsPartitionEOF { get; private set; }

        public ConsumeResult(string topicName, Message<TKey, TValue> message)
        {
            TopicName = topicName;
            Message = message;
            IsPartitionEOF = false;
        }
    }
}
