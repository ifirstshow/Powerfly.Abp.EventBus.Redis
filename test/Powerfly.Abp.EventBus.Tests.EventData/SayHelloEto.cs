using Volo.Abp.EventBus;

namespace Powerfly.Abp.EventBus.Tests.EventData
{
    [EventName("Powerfly.Abp.Message")]
    public class SayHelloEto
    {
        public int Count { get; set; }
    }
}
