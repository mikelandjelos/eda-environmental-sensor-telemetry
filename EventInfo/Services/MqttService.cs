using System.Text;
using EventInfo.Configuration;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
namespace EventInfo.Services;

public class MqttService : BackgroundService
{
    private readonly ILogger<MqttService> _logger;
    private readonly IMqttClient _mqttClient;

    public MqttService(ILogger<MqttService> logger, IOptions<MqttSettings> mqttSettings)
    {
        _logger = logger;
        _mqttClient = new MqttFactory().CreateMqttClient();

        string broker = Environment.GetEnvironmentVariable("MQTT_BROKER_HOST") ?? mqttSettings.Value.Broker;
        int port = int.TryParse(Environment.GetEnvironmentVariable("MQTT_BROKER_PORT"), out var parsedPort)
            ? parsedPort : mqttSettings.Value.Port;
        string topic = Environment.GetEnvironmentVariable("MQTT_EVENTS_TOPIC") ?? mqttSettings.Value.Topic;

        _logger.LogDebug($"Broker: `{broker}`\nPort: {port}\nTopic: `{topic}`");

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(broker, port)
            .WithClientId($"event-info-{new Random().Next(0x1000000).ToString("X6")}")
            .WithCleanSession()
            .Build();

        var connectResult = _mqttClient.ConnectAsync(options).Result;

        if (connectResult.ResultCode != MqttClientConnectResultCode.Success)
            throw new Exception($"Couldn't connect to MQTT broker over TCP on address `{broker}:{port}`,"
            + $" reason: {connectResult.ReasonString}");

        _mqttClient.ApplicationMessageReceivedAsync += e =>
        {
            _logger.LogDebug($"Received message ({topic}): {Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment)}");
            return Task.CompletedTask;
        };

        _ = _mqttClient.SubscribeAsync(topic).Result;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MqttService is starting.");

        // Indefinite delay
        var completionSource = new TaskCompletionSource<object>();
        stoppingToken.Register(() => completionSource.SetResult(null!));
        await completionSource.Task;

        _logger.LogInformation("MqttService  is stopping.");
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MqttService is stopping.");
        await base.StopAsync(stoppingToken);
    }
}