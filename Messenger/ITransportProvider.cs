namespace Messenger;

public interface IRequestForwarder
{
    Task<string> SendRequest(string name, string request, CancellationToken cancellationToken);
}

public interface IMessageForwarder
{
    Task SendMessage(string name, string message, CancellationToken cancellationToken);
}

public interface IAsyncRequestForwarder
{
    Task SendRequest(string id, string name, string message, CancellationToken cancellationToken);
}
