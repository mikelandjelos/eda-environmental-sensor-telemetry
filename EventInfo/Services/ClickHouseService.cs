using EventInfo.Configurations;
using Microsoft.Extensions.Options;
using Octonica.ClickHouseClient;

namespace EventInfo.Services
{
    public class ClickHouseService : IDisposable
    {
        private readonly ILogger<ClickHouseService> _logger;
        private readonly ClickHouseConnection _clickHouseConnection;
        private bool _disposed = false;

        public ClickHouseService(ILogger<ClickHouseService> logger, IOptions<ClickHouseSettings> clickHouseSettings)
        {
            _logger = logger;

            var sb = new ClickHouseConnectionStringBuilder();
            sb.Host = Environment.GetEnvironmentVariable("CLICKHOUSE_HOST") ?? clickHouseSettings.Value.Host;
            sb.Port = ushort.TryParse(Environment.GetEnvironmentVariable("CLICKHOUSE_PORT"), out var parsedClickHousePort)
                ? parsedClickHousePort : clickHouseSettings.Value.Port;

            _clickHouseConnection = new ClickHouseConnection(sb);
        }

        public async Task<bool> TryPingAsync()
        {
            await _clickHouseConnection.OpenAsync();
            return await _clickHouseConnection.TryPingAsync();
        }

        public async Task<ClickHouseCommand> CreateCommand(string command)
        {
            await _clickHouseConnection.OpenAsync();
            return _clickHouseConnection.CreateCommand(command);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _logger.LogDebug("ClickHouse disposed!");
                    _clickHouseConnection?.Dispose();
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
