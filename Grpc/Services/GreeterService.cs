using System.Diagnostics;
using Grpc.Core;
using Grpc;
using Jeager;
using Jeager.DatabaseContext;
using Microsoft.EntityFrameworkCore;

namespace Grpc.Services;

public class GreeterService : Greeter.GreeterBase
{
    private readonly ILogger<GreeterService> _logger;
    private static readonly ActivitySource ActivitySource = new("Grpc.Api");
    private readonly JeagerDbContext _dbContext;

    public GreeterService(ILogger<GreeterService> logger, JeagerDbContext dbContext
    )
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public override async Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        using var activity = ActivitySource.StartActivity("SayHello_Action");
        activity?.SetTag("http.method", "POST");
        activity?.SetTag("http.request", request);

        var entity =await _dbContext.Tests.FirstOrDefaultAsync(x=>x.Name == request.Name);
        if (entity != null)
        {
            return await Task.FromResult(new HelloReply
            {
                Message = "Hello " + entity.Name,
                Id = entity.Id ?? 0
            });
        }
        var test = await _dbContext.Tests.AddAsync(new Test() { Name = request.Name });
        await _dbContext.SaveChangesAsync();

        return await Task.FromResult(new HelloReply
        {
            Message = "Hello " + request.Name,
            Id = test.Entity.Id ?? 0
        });
    }
}