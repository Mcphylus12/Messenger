using Demo.Common;
using Messenger;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMessenger(o => o.RegisterAssemblies(
    typeof(TestMessage).Assembly,
    typeof(DemoForwarder).Assembly
    ));
builder.Services.AddHostedService<Runner>();
builder.Services.AddHttpClient();

var app = builder.Build();

/// Async response handler
app.MapPost("/", async (HttpContext context, [FromServices] Messenger.IRouter router, [FromServices] HttpClient httpClient) =>
{
    var request = await context.Request.ReadFromJsonAsync<MessagePacket>();
    await router.Receive(request!.Name, request!.Data, CancellationToken.None);
    return Results.Ok();
});

app.UseHttpsRedirection();
app.Run();
