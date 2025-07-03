using Demo.Common;
using Messenger;

namespace Client;

public class DemoMessageForwarder : IMessageForwarder
{
    private readonly HttpClient _httpClient;

    public DemoMessageForwarder(HttpClient httpClient)
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
}

public class DemoRequestForwarder : IRequestForwarder
{
    private readonly HttpClient _httpClient;

    public DemoRequestForwarder(HttpClient httpClient)
    {
        _httpClient = httpClient;
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
}

public class DemoAsyncRequestForwarder : IAsyncRequestForwarder
{
    private readonly HttpClient _httpClient;

    public DemoAsyncRequestForwarder(HttpClient httpClient)
    {
        _httpClient = httpClient;
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