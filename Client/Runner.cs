
using Demo.Common;
using Messenger;

public class Runner : BackgroundService
{
    private readonly ISender _sender;
    private readonly ILogger<Runner> _logger;

    public Runner(ISender sender, ILogger<Runner> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Task.Run(async () =>
        {
            await Task.Delay(1000);

            try
            {
                await _sender.Send(new TestMessage("Some Message Stuff"));
                _logger.LogInformation("Sent Message");
                var response = await _sender.Send(new AddRequest(10, 6));
                _logger.LogInformation("Sent Add Request Got response: " + response.Result);
                var stringResponse = await _sender.Send(new StringLowerRequest("HELLO"));
                _logger.LogInformation("Sent String Request got response: " + stringResponse.Result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error");
            }
        });

        return Task.CompletedTask;
    }
}

public class DemoForwarder : IMessageForwarder, IRequestForwarder, IAsyncRequestForwarder
{
    private readonly HttpClient _httpClient;

    public DemoForwarder(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task SendMessage(string name, string message, CancellationToken cancellationToken)
    {
        return _httpClient.PostAsJsonAsync("http://localhost:8081", new MessagePacket
        {
            Data = message,
            Name = name
        });
    }

    public async Task<string> SendRequest(string name, string request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync("http://localhost:8081", new MessagePacket
        {
            Data = request,
            Name = name
        });

        return (await response.Content.ReadFromJsonAsync<MessagePacket>())!.Data;
    }

    public Task SendRequest(string id, string name, string message, CancellationToken cancellationToken)
    {
        return _httpClient.PostAsJsonAsync("http://localhost:8081", new MessagePacket
        {
            Data = message,
            Name = name,
            Id = id
        });
    }
}