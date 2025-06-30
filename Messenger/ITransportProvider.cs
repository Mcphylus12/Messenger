namespace Messenger;

public interface ITransportProvider
{
    Task<string> SendRequest(string name, string request, CancellationToken cancellationToken);
    Task SendMessage(string name, string message, CancellationToken cancellationToken);
}
