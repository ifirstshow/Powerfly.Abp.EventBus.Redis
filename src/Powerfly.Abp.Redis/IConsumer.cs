namespace Powerfly.Abp.Redis
{
    public interface IConsumer<TKey, TValue> : IDisposable
    {
        Task CommitAsync(ConsumeResult<TKey, TValue> consumeResult);
        Task<ConsumeResult<TKey, TValue>> ConsumeAsync();
        void Subscribe(string topicName);
        void Unsubscribe();
        void Close();
    }
}
