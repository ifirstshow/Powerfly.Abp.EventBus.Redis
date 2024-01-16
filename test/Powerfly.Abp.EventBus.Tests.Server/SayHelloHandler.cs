using Powerfly.Abp.EventBus.Tests.EventData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace Powerfly.Abp.EventBus.Tests.Server
{
    public class SayHelloHandler
        : IDistributedEventHandler<SayHelloEto>,
          ITransientDependency
    {
        public async Task HandleEventAsync(SayHelloEto eventData)
        {
            Console.WriteLine($"Say hello {eventData.Count}.");
        }
    }
}
