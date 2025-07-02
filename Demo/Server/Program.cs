using Demo.Common;
using Messenger;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMessenger(o => o.RegisterAssemblies(typeof(TestMessage).Assembly));
builder.Services.AddHttpClient();

var app = builder.Build();

app.MapPost("/", async (HttpContext context, [FromServices]Messenger.IRouter router, [FromServices] HttpClient httpClient) =>
{
    var request = await context.Request.ReadFromJsonAsync<MessagePacket>();

    var response = await router.Receive(request!.Name, request!.Data, CancellationToken.None);

    if (request.Id is not null && response is not null)
    {
        // Mimic a delayed execution with a webhook to call when work is completed for async workflow 
        Task.Run(async () =>
        {
            await Task.Delay(5000);

            await httpClient.PostAsJsonAsync("http://localhost:8080", new MessagePacket
            {
                Data = response,
                Id = request.Id
            });
        });

        return Results.Ok();
    }
    else if (response is not null)
    {
        return Results.Ok(new MessagePacket
        {
            Data = response,
            Name = request.Name
        });
    } 
    else
    {
        return Results.Ok();
    }
});

app.Run();