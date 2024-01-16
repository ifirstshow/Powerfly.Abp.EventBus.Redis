namespace Powerfly.Abp.Redis
{
    public class Message<TKey, TValue>
    {
        public Headers Headers { get; set; } = [];

        public TKey Key { get; set; } = default!;

        public TValue Value { get; set; } = default!;

        public Message()
        {
        }

        public Message(Headers headers, TKey key, TValue value) {
            Headers = headers ?? throw new ArgumentNullException(nameof(headers));
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}
