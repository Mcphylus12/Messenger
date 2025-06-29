using Microsoft.Extensions.DependencyInjection;

namespace Messenger;

public interface IMessage
{
}

public interface IMessageHandlerWrapper
{
    Task Handle(object message, CancellationToken cancellationToken);
}


public class MessageHandlerWrapper<TMessage, THandler> : IMessageHandlerWrapper
    where THandler : IHandler<TMessage>
{
    private readonly IServiceProvider _serviceProvider;

    public MessageHandlerWrapper(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task Handle(object request, CancellationToken cancellationToken)
    {
        var handler = ActivatorUtilities.CreateInstance<THandler>(_serviceProvider);
        return handler.Handle((TMessage)request, cancellationToken);
    }
}

public interface IHandler<TMessage>
{
    Task Handle(TMessage request, CancellationToken cancellationToken);
}