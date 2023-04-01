using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.eShopWeb.ApplicationCore.DTO;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;

namespace DeliveryOrderProcessor
{
    public static class DeliveryOrderProcessor
    {
        private static readonly string DbEndpointUri = Environment.GetEnvironmentVariable("CosmosDbEndpointUri");
        private static readonly string DbPrimaryKey = Environment.GetEnvironmentVariable("CosmosDbPrimaryKey");
        private static readonly string DbName = Environment.GetEnvironmentVariable("CosmosDbName");
        private static readonly string ContainerName = Environment.GetEnvironmentVariable("CosmosDbContainerName");

        [FunctionName("DeliveryOrderProcessor")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            HttpRequest req, ILogger log)
        {
            log.LogInformation($"{nameof(DeliveryOrderProcessor)} processing a request.");

            if (req.Body.Length == 0)
            {
                return new BadRequestResult();
            }

            string requestBody = await req.ReadAsStringAsync();

            log.LogInformation("Request body: {0}", requestBody);

            var deliveryItem = JsonSerializer.Deserialize<DeliveryDetailsDto>(requestBody);

            var response = await PersistDeliveryInformation(deliveryItem);

            return new StatusCodeResult((int)response.StatusCode);
        }

        private static async Task<ItemResponse<DeliveryDetailsRecord>> PersistDeliveryInformation(DeliveryDetailsDto deliveryItem)
        {
            CosmosClient client = new(DbEndpointUri, DbPrimaryKey, new CosmosClientOptions
            {
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            });

            var response = await client.CreateDatabaseIfNotExistsAsync(DbName);
            var container = await response.Database.CreateContainerIfNotExistsAsync(ContainerName, "/partitionKey");

            var record = DeliveryDetailsRecord.FromDeliveryDetailsDto(deliveryItem);

            var result = await container.Container.CreateItemAsync(record, new PartitionKey(record.PartitionKey));

            return result;
        }
    }
}
