using Client;
using Demo.Common;
using Messenger;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMessenger(o => o.RegisterAssemblies(
    typeof(TestMessage).Assembly,
    typeof(DemoAsyncRequestForwarder).Assembly
    ));
builder.Services.AddHostedService<Runner>();
builder.Services.AddHttpClient();

var app = builder.Build();

/// Async response handler
app.MapPost("/", async (HttpContext context, [FromServices] Messenger.IRouter router, [FromServices] HttpClient httpClient) =>
{
    Console.WriteLine("Async webhook called");
    var request = await context.Request.ReadFromJsonAsync<MessagePacket>();
    await router.Receive(request!.Id, request!.Data, CancellationToken.None);
    return Results.Ok();
});

app.UseHttpsRedirection();
app.Run();
