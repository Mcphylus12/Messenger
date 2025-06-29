namespace Messenger;

public interface ITransportProvider
{
    Task<TResponse> SendRequest<TResponse>(string name, IRequest<TResponse> request, CancellationToken cancellationToken);
    Task SendMessage(string name, IMessage message, CancellationToken cancellationToken);
}
