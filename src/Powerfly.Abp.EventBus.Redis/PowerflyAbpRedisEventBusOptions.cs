namespace Powerfly.Abp.EventBus.Redis
{
    public class PowerflyAbpRedisEventBusOptions
    {
        public string? ConnectionName { get; set; }

        public string TopicName { get; set; } = default!;

        public string GroupName { get; set; } = string.Empty;
    }
}
