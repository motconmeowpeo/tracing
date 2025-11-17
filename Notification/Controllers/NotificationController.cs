using Grpc;
using Microsoft.AspNetCore.Mvc;

namespace Notification.Controllers;

[ApiController]
[Route("[controller]")]
public class NotificationController : ControllerBase
{
    private readonly ILogger<NotificationController> _logger;
    private readonly Greeter.GreeterClient _greatClient;

    public NotificationController(ILogger<NotificationController> logger, Greeter.GreeterClient greatClient)
    {
        _logger = logger;
        _greatClient = greatClient;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok("Get Notification Service");
    }

    [HttpPost]
    public async Task<IActionResult> Grpc()
    {
        var value = await _greatClient.SayHelloAsync(new HelloRequest { Name = "Notification" });
        return Ok(value);
    }
}