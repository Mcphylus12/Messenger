using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Messenger;

public interface ISender
{
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    public Task Send(IMessage message, CancellationToken cancellationToken = default);
}

public interface IRouter
{
    public Task<string?> Receive(string name, string data, CancellationToken cancellationToken);
}

public class Messenger : ISender, IRouter
{
    private readonly MessageConfiguration _configuration;
    private Dictionary<string, TaskCompletionSource<string>> _inFlightAsyncRequests = new();

    public Messenger(MessageConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<string?> Receive(string messagingRoutingKey, string data, CancellationToken cancellationToken)
    {
        if (_inFlightAsyncRequests.TryGetValue(messagingRoutingKey, out var inFlightRequest))
        {
            inFlightRequest.SetResult(data);
            _inFlightAsyncRequests.Remove(messagingRoutingKey);
            return null;
        }

        var type = _configuration.GetRequestType(messagingRoutingKey);

        if (type is null)
        {
            throw new MessagingException($"No handler setup for {messagingRoutingKey}");
        }

        var inBoundData = JsonSerializer.Deserialize(data, type);

        if (inBoundData is null)
        {
            throw new MessagingException("Failure to deserialise inbound message");
        }

        if (_configuration.GetHandler(messagingRoutingKey) is IRequestHandlerWrapper requestHandler)
        {
            var response = await requestHandler.Handle(inBoundData, cancellationToken);
            return JsonSerializer.Serialize(response);
        }

        if (_configuration.GetHandler(messagingRoutingKey) is IMessageHandlerWrapper messageHandler)
        {
            await messageHandler.Handle(inBoundData, cancellationToken);
            return null;
        }

        throw new MessagingException($"No handler setup for {messagingRoutingKey}");
    }

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var messagingName = MessageNameAttribute.GetMessagingName(request.GetType());
        object? provider = _configuration.GetTransport(messagingName);

        if (provider is IRequestForwarder requestForwarder)
        {
            var requestData = JsonSerializer.Serialize(request, request.GetType());
            var responseData = await requestForwarder.SendRequest(messagingName, requestData, cancellationToken);
            return JsonSerializer.Deserialize<TResponse>(responseData)!;
        }

        if (provider is IAsyncRequestForwarder asyncRequestForwarder)
        {
            var requestData = JsonSerializer.Serialize(request, request.GetType());
            var messageId = Guid.NewGuid().ToString();
            _inFlightAsyncRequests[messageId] = new TaskCompletionSource<string>();
            await asyncRequestForwarder.SendRequest(messageId, messagingName, requestData, cancellationToken);
            var responseData = await _inFlightAsyncRequests[messageId].Task;
            return JsonSerializer.Deserialize<TResponse>(responseData)!;
        }

        if (_configuration.GetHandler(messagingName) is IRequestHandlerWrapper handler)
        {
            return (TResponse)await handler.Handle(request, cancellationToken);
        }

        throw new MessagingException($"{messagingName} was dispatched but did not map to a handler or transport provider");
    }

    public async Task Send(IMessage message, CancellationToken cancellationToken = default)
    {
        var messagingName = MessageNameAttribute.GetMessagingName(message.GetType());
        object? provider = _configuration.GetTransport(messagingName);

        if (provider is IMessageForwarder messageForwarder)
        {
            var messageData = JsonSerializer.Serialize(message, message.GetType());
            await messageForwarder.SendMessage(messagingName, messageData, cancellationToken);
            return;
        }

        if (_configuration.GetHandler(messagingName) is IMessageHandlerWrapper handler)
        {
            await handler.Handle(message, cancellationToken);
            return;
        }

        throw new MessagingException($"{messagingName} was dispatched but did not map to a handler or transport provider");
    }
}

public class MessageConfiguration
{
    private Dictionary<string, Type> _handlers = [];
    private Dictionary<string, Type> _transportConfig = [];
    private readonly IServiceProvider _serviceProvider;

    public MessageConfiguration(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public MessageConfiguration Register<T>()
    {
        var interfaces = typeof(T).GetInterfaces();

        foreach (var iinterface in interfaces)
        {
            if (iinterface.Name.StartsWith("IHandler`2"))
            {
                _handlers[MessageNameAttribute.GetMessagingName(iinterface.GenericTypeArguments[0])]
                    = typeof(RequestHandlerWrapper<,,>).MakeGenericType([.. iinterface.GenericTypeArguments, typeof(T)]);
            }

            if (iinterface.Name.StartsWith("IHandler`1"))
            {
                _handlers[MessageNameAttribute.GetMessagingName(iinterface.GenericTypeArguments[0])]
                    = typeof(MessageHandlerWrapper<,>).MakeGenericType([.. iinterface.GenericTypeArguments, typeof(T)]);
            }
        }

        return this;
    }

    internal object? GetTransport(string messagingName)
    {
        if (_transportConfig.TryGetValue(messagingName, out var type))
        {
            return ActivatorUtilities.CreateInstance(_serviceProvider, type);
        }

        return null;
    }

    internal object? GetHandler(string messagingName)
    {
        if (_handlers.TryGetValue(messagingName, out var handler))
        {
            return ActivatorUtilities.CreateInstance(_serviceProvider, handler);
        }

        return null;
    }

    public MessageConfiguration WithTransport<TTransportProvider>(params Type[] requestTypes)
    {
        foreach (var item in requestTypes)
        {
            _transportConfig[MessageNameAttribute.GetMessagingName(item)] = typeof(TTransportProvider);
        }

        return this;
    }

    internal Type? GetRequestType(string name)
    {
        if (_handlers.TryGetValue(name, out var handlerType))
        {
            return handlerType.GenericTypeArguments[0];
        }

        return null;
    }
}
