using Messenger;
using Microsoft.Extensions.DependencyInjection;

namespace Tests;
public class AsyncTransportTests
{
    private readonly ISender _outBoundMessenger;
    private readonly TestState _testState;

    public AsyncTransportTests()
    {
        _testState = new TestState();

        var _outboundServices = new ServiceCollection()
            .AddSingleton<AsyncTransportDummy>()
            .AddMessenger(o =>
            {
                o.RegisterAssemblies(typeof(TestRequestHandler).Assembly);
                o.Load(new JsonMessagingConfig
                {
                    Forwarders = new Dictionary<string, List<string>>
                    {
                        [nameof(TestAsyncTransport)] = new List<string>
                        {
                            nameof(TestRequest),
                            nameof(TestMessage)
                        }
                    }
                });
            })
            .BuildServiceProvider();
        _outBoundMessenger = _outboundServices.GetRequiredService<ISender>();

        var _inboundServices = new ServiceCollection()
            .AddSingleton(_testState)
            .AddMessenger(o => o.RegisterAssemblies(typeof(TestRequestHandler).Assembly))
            .BuildServiceProvider();
        var inBoundMessenger = _inboundServices.GetRequiredService<IRouter>();

        var transportDummy = _outboundServices.GetRequiredService<AsyncTransportDummy>();
        transportDummy.OnReceive = inBoundMessenger.Receive;
    }

    [Fact]
    public async Task RequestTest()
    {
        var response = await _outBoundMessenger.Send(new TestRequest()
        {
            In = 6
        });

        Assert.Equal(7, response.Out);
    }
}

public class TestAsyncTransport : IAsyncRequestForwarder
{
    private readonly AsyncTransportDummy _asyncTransportDummy;

    public TestAsyncTransport(AsyncTransportDummy asyncTransportDummy)
    {
        _asyncTransportDummy = asyncTransportDummy;
    }

    public Task SendRequest(string id, string name, string message, CancellationToken cancellationToken)
    {
        _asyncTransportDummy.Send(id, name, message, cancellationToken);
        return Task.CompletedTask;
    }
}

public class AsyncTransportDummy
{
    private readonly IRouter _responseRouter;

    public Func<string, string, CancellationToken, Task<string?>> OnReceive { get; set; } = null!;

    public AsyncTransportDummy(IRouter responseRouter)
    {
        _responseRouter = responseRouter;
    }

    internal void Send(string id, string name, string request, CancellationToken cancellationToken)
    {
        OnReceive?.Invoke(name, request, cancellationToken).ContinueWith(t =>
        {
            _responseRouter.Receive(id, t.Result!, CancellationToken.None);
        });
    }
}
