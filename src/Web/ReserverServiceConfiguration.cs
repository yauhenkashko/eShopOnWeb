using System;

public class ReserverServiceConfiguration
{
    public static string ConfigurationName = "ReserverService";

    public bool Enabled { get; set; } = false;
    public string? ConnectionString { get; set; }
    public string? QueueName { get; set; }
}
