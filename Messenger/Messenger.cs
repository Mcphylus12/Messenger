using Microsoft.Extensions.DependencyInjection;

namespace Messenger;

public interface ISender
{
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    public Task Send(IMessage message, CancellationToken cancellationToken = default);
}

public class Messenger : ISender
{
    private readonly MessageConfiguration _configuration;

    public Messenger(MessageConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var messagingName = MessageNameAttribute.GetMessagingName(request.GetType());
        ITransportProvider? provider = _configuration.GetTransport(messagingName);

        if (provider is not null)
        {
            return await provider.SendRequest(messagingName, request, cancellationToken);
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
            await provider.SendMessage(messagingName, message, cancellationToken);
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
    private Dictionary<string, ITransportProvider> _transportConfig = [];
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
        return _transportConfig.GetValueOrDefault(messagingName);
    }

    internal object? GetHandler(string messagingName)
    {
        if (_handlers.TryGetValue(messagingName, out var handler))
        {
            return ActivatorUtilities.CreateInstance(_serviceProvider, handler);
        }

        return null;
    }

    public MessageConfiguration WithTransport(ITransportProvider transport, Type[] requestTypes)
    {
        foreach (var item in requestTypes)
        {
            _transportConfig[MessageNameAttribute.GetMessagingName(item)] = transport;
        }

        return this;
    }
}
