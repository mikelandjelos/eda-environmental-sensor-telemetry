namespace EventInfo.Configuration;

public class MqttSettings
{
    public string Broker { get; set; } = null!;
    public int Port { get; set; }
    public string Topic { get; set; } = null!;
}