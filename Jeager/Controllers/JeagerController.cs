using System.Diagnostics;
using Grpc;
using Jeager.DatabaseContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jeager.Controllers;

[ApiController]
[Route("[controller]/api/v1")]
public class JeagerController : ControllerBase
{
    private readonly ILogger<JeagerController> _logger;
    private readonly Greeter.GreeterClient _greatClient;
    private readonly JeagerDbContext _dbContext;

    public JeagerController(ILogger<JeagerController> logger, Greeter.GreeterClient greatClient,JeagerDbContext dbContext)
    {
        _logger = logger;
        _greatClient = greatClient;
        _dbContext = dbContext;
    }

    [HttpGet("test-get")]
    public IActionResult Get()
    {    

        return Ok("Get Jeager Service");
    }

    [HttpPost("test-grpc")]
    public async Task<IActionResult> Grpc()
    {
        var value = await _greatClient.SayHelloAsync(new HelloRequest { Name = "Jeager.Api" });
        if (value.Id != 0)
        {
            return Ok(value.Message);
        }

        return Ok("Jeager.Api not found");
    }
}