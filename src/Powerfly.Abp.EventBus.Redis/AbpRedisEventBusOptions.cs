namespace Powerfly.Abp.EventBus.Redis
{
    public class AbpRedisEventBusOptions
    {
        public string? ConnectionName { get; set; }

        public string TopicName { get; set; } = default!;

        public string GroupName { get; set; } = string.Empty;
    }
}
