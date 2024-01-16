using StackExchange.Redis;
using System.Text;

namespace Powerfly.Abp.Redis
{
    public class Producer<TKey, TValue> : IProducer<TKey, TValue>
    {
        private bool _isDisposed;

        public Producer(IDatabase database, IRedisSerializer serializer, ProducerConfig config)
        {
            Database = database;
            Serializer = serializer;
            Config = config;
        }

        public IDatabase Database { get; }
        public IRedisSerializer Serializer { get; }
        public ProducerConfig Config { get; }

        public async Task<DeliveryResult<string, byte[]>> ProduceAsync(string topicName, Message<string, byte[]> message)
        {
            var msg = Encoding.UTF8.GetString(Serializer.Serialize(message));
            var key = await Database.StreamAddAsync(topicName, "d", msg, "*", Config.MaxLength);
            return new DeliveryResult<string, byte[]>(topicName, key.ToString(), message);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            Database.Multiplexer.Dispose();
        }
    }
}