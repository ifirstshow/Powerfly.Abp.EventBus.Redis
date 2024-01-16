
namespace Powerfly.Abp.Redis
{
    public interface IProducer<TKey, TValue> : IDisposable
    {
        Task<DeliveryResult<string, byte[]>> ProduceAsync(string topicName, Message<string, byte[]> message);
    }
}