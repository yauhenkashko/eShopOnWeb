using Microsoft.eShopWeb.ApplicationCore.DTO;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using System.Collections.Generic;
using System;

namespace DeliveryOrderProcessor;

public class DeliveryDetailsRecord
{
    public string Id { get; set; }
    public string PartitionKey { get; set; }
    public List<DeliveryItemDto> Items { get; set; }
    public Address DeliveryAddress { get; set; }

    public DeliveryDetailsRecord(string id, string partitionKey, List<DeliveryItemDto> items, Address deliveryAddress)
    {
        Id = id;
        PartitionKey = partitionKey;
        Items = items;
        DeliveryAddress = deliveryAddress;
    }

    public static DeliveryDetailsRecord FromDeliveryDetailsDto(DeliveryDetailsDto details)
    {
        if (details?.DeliveryAddress?.City is null)
        {
            throw new ArgumentNullException(nameof(details));
        }

        return new DeliveryDetailsRecord(details.Id.ToString(), details.DeliveryAddress.City, details.Items, details.DeliveryAddress);
    }
}