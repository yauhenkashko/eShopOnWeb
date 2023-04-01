using System;

public class DeliveryServiceConfiguration
{
    public static string ConfigurationName = "DeliveryService";

    public bool Enabled { get; set; } = false;

    public string? DeliveryServiceUrl { get; set; }
}
