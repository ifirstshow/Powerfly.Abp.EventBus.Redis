namespace Powerfly.Abp.Redis;

public interface IProducerPool : IDisposable
{
    IProducer<string, byte[]> Get(string? connectionName = null);
}
