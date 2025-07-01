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
        var transportDummy = new AsyncTransportDummy();

        var _outboundServices = new ServiceCollection()
            .AddSingleton(transportDummy)
            .AddMessenger(o => o.WithTransport<TestAsyncTransport>(typeof(TestRequest), typeof(TestMessage)))
            .BuildServiceProvider();
        _outBoundMessenger = _outboundServices.GetRequiredService<ISender>();

        var _inboundServices = new ServiceCollection()
            .AddSingleton(_testState)
            .AddMessenger(o => o.Register<TestRequestHandler>().Register<TestMessageHandler>())
            .BuildServiceProvider();
        var inBoundMessenger = _inboundServices.GetRequiredService<IRouter>();

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

public class TestAsyncTransport : AsyncTransportProvider
{
    private readonly AsyncTransportDummy _asyncTransportDummy;

    public TestAsyncTransport(AsyncTransportDummy asyncTransportDummy, AsyncRequestTracker asyncRequestTracker)
        : base(asyncRequestTracker)
    {
        _asyncTransportDummy = asyncTransportDummy;
    }

    // Messaging is not different for "async" work as its about how request/response is handled
    public override Task SendMessage(string name, string message, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public override Task SendRequest(string id, string name, string request, CancellationToken cancellationToken)
    {
        _asyncTransportDummy.Send(this, id, name, request, cancellationToken);
        return Task.CompletedTask;
    }

    public void Handle(string id, string response, CancellationToken cancellationToken)
    {
        base.HandleResponse(id, response, cancellationToken);
    }
}

public class AsyncTransportDummy
{
    public Func<string, string, CancellationToken, Task<string?>> OnReceive { get; set; } = null!;

    internal void Send(TestAsyncTransport testAsyncTransport, string id, string name, string request, CancellationToken cancellationToken)
    {
        OnReceive?.Invoke(name, request, cancellationToken).ContinueWith(t =>
        {
            testAsyncTransport.Handle(id, t.Result!, CancellationToken.None);
        });
    }
}
