namespace Powerfly.Abp.Redis;

public interface IConsumerPool : IDisposable
{
    IConsumer<string, byte[]> Get(string groupId, string? connectionName = null);
}
