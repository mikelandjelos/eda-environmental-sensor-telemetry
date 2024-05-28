using Microsoft.AspNetCore.Mvc;
using EventInfo.Models;
using EventInfo.Services;

namespace EventInfo.Controllers;

[ApiController]
[Route("[controller]")]
public class EventInfoController : ControllerBase
{
    private readonly ILogger<EventInfoController> _logger;

    public EventInfoController(ILogger<EventInfoController> logger)
    {
        _logger = logger;
    }

    [HttpGet("/GetEvents")]
    public IEnumerable<SensorEvent> GetEvents([FromQuery] int offset, [FromQuery] int limit)
    {
        _logger.LogInformation($"Offset: {offset}, Limit: {limit}");
        throw new NotImplementedException(this.GetType().FullName + ".GetEvents");
    }
}
