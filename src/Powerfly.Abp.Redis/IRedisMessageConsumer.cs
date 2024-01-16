namespace Powerfly.Abp.Redis;

public interface IRedisMessageConsumer
{
    void OnMessageReceived(Func<Message<string, byte[]>, Task> callback);
}
