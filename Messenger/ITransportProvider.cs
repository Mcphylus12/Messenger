namespace Messenger;

public interface ITransportProvider
{
    Task<string> SendRequest(string name, string request, CancellationToken cancellationToken);
    Task SendMessage(string name, string message, CancellationToken cancellationToken);
}

public class AsyncRequestTracker : Dictionary<string, TaskCompletionSource<string>>;

public abstract class AsyncTransportProvider : ITransportProvider
{
    private readonly AsyncRequestTracker _inFlightRequests;

    protected AsyncTransportProvider(AsyncRequestTracker asyncTransportRequestTracker)
    {
        _inFlightRequests = asyncTransportRequestTracker;
    }

    public abstract Task SendMessage(string name, string message, CancellationToken cancellationToken);

    public Task<string> SendRequest(string name, string request, CancellationToken cancellationToken)
    {
        var messageId = Guid.NewGuid().ToString();
        _inFlightRequests[messageId] = new TaskCompletionSource<string>();

        SendRequest(messageId, name, request, cancellationToken);

        return _inFlightRequests[messageId].Task;
    }

    public abstract Task SendRequest(string id, string name, string request, CancellationToken cancellationToken);

    protected void HandleResponse(string id, string response, CancellationToken cancellationToken)
    {
        if (!_inFlightRequests.TryGetValue(id, out var taskCompletion))
        {
            throw new MessagingException("Unexpected response received for message id " + id);
        }

        taskCompletion.SetResult(response);
        _inFlightRequests.Remove(id);
    }
}
