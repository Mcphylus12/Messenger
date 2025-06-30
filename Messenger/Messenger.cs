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

    public Messenger(MessageConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<string?> Receive(string messagingName, string data, CancellationToken cancellationToken)
    {
        var type = _configuration.GetRequestType(messagingName);

        if (type is null)
        {
            throw new MessagingException($"No handler setup for {messagingName}");
        }

        var inBoundData = JsonSerializer.Deserialize(data, type);

        if (inBoundData is null)
        {
            throw new MessagingException("Failure to deserialise inbound message");
        }

        if (_configuration.GetHandler(messagingName) is IRequestHandlerWrapper requestHandler)
        {
            var response = await requestHandler.Handle(inBoundData, cancellationToken);
            return JsonSerializer.Serialize(response);
        }

        if (_configuration.GetHandler(messagingName) is IMessageHandlerWrapper messageHandler)
        {
            await messageHandler.Handle(inBoundData, cancellationToken);
            return null;
        }

        throw new MessagingException($"No handler setup for {messagingName}");
    }

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var messagingName = MessageNameAttribute.GetMessagingName(request.GetType());
        ITransportProvider? provider = _configuration.GetTransport(messagingName);

        if (provider is not null)
        {
            var requestData = JsonSerializer.Serialize(request, request.GetType());
            var responseData = await provider.SendRequest(messagingName, requestData, cancellationToken);
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
        ITransportProvider? provider = _configuration.GetTransport(messagingName);

        if (provider is not null)
        {
            var messageData = JsonSerializer.Serialize(message, message.GetType());
            await provider.SendMessage(messagingName, messageData, cancellationToken);
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

    internal ITransportProvider? GetTransport(string messagingName)
    {
        if (_transportConfig.TryGetValue(messagingName, out var type))
        {
            return (ITransportProvider?)ActivatorUtilities.CreateInstance(_serviceProvider, type);
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
        where TTransportProvider : ITransportProvider
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
