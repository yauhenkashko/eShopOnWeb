using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace OrderReserver
{
    public class OrderReserver
    {
        [FunctionName("OrderReserver")]
        public async Task Run([ServiceBusTrigger("reserverqueue", Connection = "OrderReserverQueueConnectionString")] string myQueueItem, string messageId, ILogger log)
        {
            log.LogInformation($"ServiceBus queue trigger function processing message: {myQueueItem}");
            log.LogInformation($"Message: {messageId}");

            var containerName = Environment.GetEnvironmentVariable("BlobStorageContainerName");
            var blobConnectionString = Environment.GetEnvironmentVariable("BlobStorageConnectionString");

            var blobServiceClient = new BlobServiceClient(blobConnectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            BlobClient blobClient = containerClient.GetBlobClient($"{messageId}.json");

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(myQueueItem)))
            {
                await blobClient.UploadAsync(stream, true);
            }

            log.LogInformation($"Message has been uploaded to the blob storage: {myQueueItem}");
        }
    }
}
