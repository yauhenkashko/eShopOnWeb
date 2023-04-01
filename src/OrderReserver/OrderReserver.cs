using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace OrderReserver
{
    public class OrderReserver
    {
        [FunctionName("OrderReserver")]
        public void Run([ServiceBusTrigger("reserverqueue", Connection = "OrderReserverQueueConnectionString")] string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");
        }
    }
}
