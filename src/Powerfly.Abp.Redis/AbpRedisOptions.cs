using StackExchange.Redis;

namespace Powerfly.Abp.Redis;

public class AbpRedisOptions
{
    public RedisConnections Connections { get; }

    public Action<ProducerConfig>? ConfigureProducer { get; set; }

    public Action<ConsumerConfig>? ConfigureConsumer { get; set; }

    public string Configuration
    {
        get
        {
            return Connections.Default.Configuration;
        }
        set
        {
            Connections.Default.Configuration = value;
        }
    }

    public AbpRedisOptions()
    {
        Connections = new RedisConnections();
    }
}
