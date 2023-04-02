using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace OrderReserver
{
    public class OrderReserver
    {
        private readonly ILogger _log;

        public OrderReserver(ILogger<OrderReserver> log)
        {
            _log = log;
        }

        [FunctionName("OrderReserver")]
        public async Task Run(
            [ServiceBusTrigger("reserverqueue", Connection = "OrderReserverQueueConnectionString")]
            string message,
            string messageId)
        {
            _log.LogInformation($"ServiceBus queue trigger function processing message: {message}");
            _log.LogInformation($"Message: {messageId}");

            try
            {
                await SendToBlobStorage(message, messageId);

                _log.LogInformation($"Message has been uploaded to the blob storage: {message}");
            }
            catch (Exception e)
            {
                _log.LogError($"Error while uploading message to blob storage: {e}");
                await SendErrorNotification(message);
            }
        }

        private async Task SendToBlobStorage(string message, string messageId)
        {
            var containerName = Environment.GetEnvironmentVariable("BlobStorageContainerName");
            var blobConnectionString = Environment.GetEnvironmentVariable("BlobStorageConnectionString");

            var blobServiceClient = new BlobServiceClient(blobConnectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient($"{messageId}.json");

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(message)))
            {
                await blobClient.UploadAsync(stream, true);
            }
        }

        private async Task SendErrorNotification(string message)
        {
            var logicAppUrl = Environment.GetEnvironmentVariable("LogicAppUrl");

            using var client = new HttpClient();
            var requestBody = new StringContent(message, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(logicAppUrl, requestBody);

            if (response.IsSuccessStatusCode)
            {
                _log.LogInformation("Logic App triggered successfully.");
            }
            else
            {
                _log.LogError($"Failed to trigger Logic App. StatusCode={response.StatusCode}, Reason={response.ReasonPhrase}");
            }
        }
    }
}
