using Microsoft.AspNetCore.Mvc;
using EventInfo.Models;
using EventInfo.Services;

namespace EventInfo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EventInfoController : ControllerBase
    {
        private readonly ILogger<EventInfoController> _logger;
        private readonly ClickHouseService _clickHouseService;

        public EventInfoController(ILogger<EventInfoController> logger, ClickHouseService clickHouseService)
        {
            _logger = logger;
            _clickHouseService = clickHouseService;
        }

        public class SensorEventResponse
        {
            public IList<SensorEvent>? Events { get; set; }
            public int Count { get; set; }
        }

        [HttpGet("/GetEvents")]
        public async Task<SensorEventResponse> GetEvents([FromQuery] int? offset, [FromQuery] int? limit)
        {
            _logger.LogInformation($"Offset: {offset}, Limit: {limit}");

            try
            {
                var sensorEvents = new List<SensorEvent>();

                var query = "SELECT * FROM environmental_sensor_telemetry.sensor_data";
                if (limit.HasValue)
                {
                    query += $" LIMIT {limit}";
                    if (offset.HasValue)
                    {
                        query += $" OFFSET {offset}";
                    }
                }

                var cmd = await _clickHouseService.CreateCommand(query);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var eventType = Enum.Parse<SensorEventType>(reader.GetString(1));
                        sensorEvents.Add(new SensorEvent
                        {
                            Timestamp = reader.GetDateTime(0),
                            Type = eventType,
                            Device = reader.GetString(2),
                            Measurement = reader.GetString(3),
                            Message = reader.GetString(4),
                            StatisticData = eventType == SensorEventType.NUMERIC ? new StatisticData
                            {
                                CurrentValue = reader.GetDouble(5),
                                DeviationNominal = reader.GetDouble(6),
                                DeviationPercent = reader.GetDouble(7),
                                RunningAverage = reader.GetDouble(8),
                                HistoryLength = reader.GetUInt32(9),
                            } : null
                        });
                    }
                }

                return new SensorEventResponse { Events = sensorEvents, Count = sensorEvents.Count };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve events from ClickHouse");
                throw;
            }
        }

    }
}
