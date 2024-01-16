using StackExchange.Redis;

namespace Powerfly.Abp.Redis;

public class PowerflyAbpRedisOptions
{
    public RedisConnections Connections { get; }

    public Action<ProducerConfig>? ConfigureProducer { get; set; }

    public Action<ConsumerConfig>? ConfigureConsumer { get; set; }

    public PowerflyAbpRedisOptions()
    {
        Connections = new RedisConnections();
    }
}
