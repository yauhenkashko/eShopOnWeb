using System;
using System.Collections.Generic;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;

namespace Microsoft.eShopWeb.ApplicationCore.DTO;

public class DeliveryDetailsDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public List<DeliveryItemDto> Items { get; set; }
    public Address? DeliveryAddress { get; set; }
}
