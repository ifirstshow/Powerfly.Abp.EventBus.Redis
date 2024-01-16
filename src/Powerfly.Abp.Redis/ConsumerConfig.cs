using StackExchange.Redis;

namespace Powerfly.Abp.Redis
{
    public class ConsumerConfig : ClientConfig
    {
        public string? GroupName { get; set; }
        public string? ConsumerName { get; set; }
        public int QueueDepth { get; set; } = 10;
        public TimeSpan ProcessingTimeout { get; set; } = TimeSpan.FromSeconds(60);
        public TimeSpan RedeliverInterval { get; set; } = TimeSpan.FromSeconds(15);
        public TimeSpan FetchInterval { get; set; } = TimeSpan.FromMilliseconds(1);

        public ConsumerConfig(ClientConfig config)
        {
            Configuration = config.Configuration;
            InstanceName = config.InstanceName;
        }
    }
}
