using Messenger;
using Microsoft.Extensions.DependencyInjection;

namespace Tests;

public class TransportTests
{
    private readonly Messenger.Messenger _outBoundMessenger;
    private readonly Messenger.Messenger _inBoundMessenger;
    private readonly TestState _testState;
    private readonly TransportDummy _transportDummy;

    public TransportTests()
    {
        _testState = new TestState();
        _transportDummy = new TransportDummy();

        var _outboundServices = new ServiceCollection()
            .AddSingleton(_transportDummy)
            .BuildServiceProvider();
        var outBoundConfig = new MessageConfiguration(_outboundServices)
            .WithTransport<TestTransport>(typeof(TestRequest), typeof(TestMessage));
        _outBoundMessenger = new Messenger.Messenger(outBoundConfig);

        var _inboundServices = new ServiceCollection()
            .AddSingleton(_testState)
            .BuildServiceProvider();
        var inBoundConfig = new MessageConfiguration(_inboundServices)
            .Register<TestRequestHandler>()
            .Register<TestMessageHandler>();
        _inBoundMessenger = new Messenger.Messenger(inBoundConfig);

        _transportDummy.OnReceive = _inBoundMessenger.Receive;
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
