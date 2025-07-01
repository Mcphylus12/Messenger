using Messenger;
using Microsoft.Extensions.DependencyInjection;

namespace Tests;

public class TransportTests
{
    private readonly ISender _outBoundMessenger;
    private readonly TestState _testState;

    public TransportTests()
    {
        _testState = new TestState();
        var transportDummy = new TransportDummy();

        var _outboundServices = new ServiceCollection()
            .AddSingleton(transportDummy)
            .AddMessenger(o => o.WithTransport<TestTransport>(typeof(TestRequest), typeof(TestMessage)))
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

    [Fact]
    public async Task MessageTest()
    {
        _testState.Called = false;
        await _outBoundMessenger.Send(new TestMessage());
        Assert.True(_testState.Called);
    }
}

public class TransportDummy
{
    public Func<string, string, CancellationToken, Task<string?>> OnReceive { get; set; } = null!;
}

public class TestTransport : ITransportProvider
{
    private readonly TransportDummy _transportDummy;

    public TestTransport(TransportDummy transportDummy)
    {
        _transportDummy = transportDummy;
    }

    public async Task SendMessage(string name, string message, CancellationToken cancellationToken)
    {
        await _transportDummy.OnReceive(name, message, cancellationToken);
    }

    public async Task<string> SendRequest(string name, string request, CancellationToken cancellationToken)
    {
        var response = await _transportDummy.OnReceive(name, request, cancellationToken);

        return response!;
    }
}
