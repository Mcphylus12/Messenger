
using Microsoft.Extensions.DependencyInjection;

namespace Messenger;

public interface IRequest<TResponse>
{
}

public interface IRequestHandlerWrapper
{
    Task<object> Handle(object request, CancellationToken cancellationToken);
}

public class RequestHandlerWrapper<TRequest, TResponse, THandler> : IRequestHandlerWrapper
    where THandler : IHandler<TRequest, TResponse>
{
    private readonly IServiceProvider _serviceProvider;

    public RequestHandlerWrapper(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<object> Handle(object request, CancellationToken cancellationToken)
    {
        var handler = ActivatorUtilities.CreateInstance<THandler>(_serviceProvider);
        return (await handler.Handle((TRequest)request, cancellationToken))!;
    }
}

public interface IHandler<TRequest, TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}