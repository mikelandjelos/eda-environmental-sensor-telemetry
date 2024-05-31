using System.Text.Json;
using System.Text.Json.Serialization;
using EventInfo.Configurations;
using EventInfo.Models;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
namespace EventInfo.Services;

public class MqttService : BackgroundService
{
    private readonly ILogger<MqttService> _logger;
    private readonly IMqttClient _mqttClient;

    private readonly ClickHouseService _clickHouseService;

    public MqttService(ILogger<MqttService> logger, IOptions<MqttSettings> mqttSettings, ClickHouseService clickHouseService)
    {
        _clickHouseService = clickHouseService;
        _logger = logger;
        _mqttClient = new MqttFactory().CreateMqttClient();

        string broker = Environment.GetEnvironmentVariable("MQTT_BROKER_HOST") ?? mqttSettings.Value.Broker;
        int port = int.TryParse(Environment.GetEnvironmentVariable("MQTT_BROKER_PORT"), out var parsedBrokerPort)
            ? parsedBrokerPort : mqttSettings.Value.Port;
        string topic = Environment.GetEnvironmentVariable("MQTT_EVENTS_TOPIC") ?? mqttSettings.Value.Topic;

        _logger.LogDebug($"Broker: `{broker}`\nPort: {port}\nTopic: `{topic}`");

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(broker, port)
            .WithClientId($"event-info-{new Random().Next(0x1000000).ToString("X6")}")
            .WithCleanSession()
            .Build();

        _mqttClient.ConnectedAsync += async e =>
        {
            var pingClickhouse = await clickHouseService.TryPingAsync();
            _logger.LogInformation($"Connected to {broker}:{port} on topic {topic}.");
            _logger.LogInformation($"Pinged clickhouse..");
        };

        var connectResult = _mqttClient.ConnectAsync(options).Result;

        if (connectResult.ResultCode != MqttClientConnectResultCode.Success)
            throw new Exception($"Couldn't connect to MQTT broker over TCP on address `{broker}:{port}`,"
            + $" reason: {connectResult.ReasonString}");

        _mqttClient.ApplicationMessageReceivedAsync += async e =>
        {
            string jsonPayload = e.ApplicationMessage.ConvertPayloadToString();
            _logger.LogInformation($"Message received: {jsonPayload}");

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
            };

            SensorEvent? sensorEvent = JsonSerializer.Deserialize<SensorEvent>(jsonPayload, options);

            string queryNumeric = $"INSERT INTO environmental_sensor_telemetry.sensor_data (timestamp, type, device, measurement, message, current_value, deviation_nominal, deviation_percent, running_average, history_length) "
                    + $"VALUES ('{sensorEvent?.Timestamp:yyyy-MM-dd HH:mm:ss}', '{sensorEvent?.Type}', '{sensorEvent?.Device}', '{sensorEvent?.Measurement}', '{sensorEvent?.Message?.Replace("'", "''")}', "
                    + $"{sensorEvent?.StatisticData?.CurrentValue}, {sensorEvent?.StatisticData?.DeviationNominal}, {sensorEvent?.StatisticData?.DeviationPercent}, "
                    + $"{sensorEvent?.StatisticData?.RunningAverage}, {sensorEvent?.StatisticData?.HistoryLength});";

            string queryBinary = $"INSERT INTO environmental_sensor_telemetry.sensor_data (timestamp, type, device, measurement, message) " +
                    $"VALUES ('{sensorEvent?.Timestamp:yyyy-MM-dd HH:mm:ss}', '{sensorEvent?.Type}', '{sensorEvent?.Device}', '{sensorEvent?.Measurement}', '{sensorEvent?.Message?.Replace("'", "''")}');";

            using var command = await _clickHouseService.CreateCommand(sensorEvent?.Type == SensorEventType.NUMERIC ? queryNumeric : queryBinary);

            _logger.LogInformation($"Executing: {command.CommandText}");
            await command.ExecuteNonQueryAsync();
            _logger.LogInformation($"Executed: {command.CommandText}");
        };

        _ = _mqttClient.SubscribeAsync(topic).Result;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MqttService is starting.");

        // Run indefinitely
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