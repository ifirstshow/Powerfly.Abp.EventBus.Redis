namespace Powerfly.Abp.Redis;

public interface IRedisMessageConsumerFactory
{
    /// <summary>
    /// Creates a new <see cref="IRedisMessageConsumer"/>.
    /// Avoid to create too many consumers since they are
    /// not disposed until end of the application.
    /// </summary>
    /// <param name="topicName"></param>
    /// <param name="groupId"></param>
    /// <param name="connectionName"></param>
    /// <returns></returns>
    IRedisMessageConsumer Create(
        string topicName,
        string groupId,
        string? connectionName = null);
}
