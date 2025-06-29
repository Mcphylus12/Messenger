using Messenger;
using Microsoft.Extensions.DependencyInjection;

namespace Tests;

public class InMemoryTests
{
    private readonly Messenger.Messenger _sut;

    public InMemoryTests()
    {
        var sp = new ServiceCollection()
            .BuildServiceProvider();
        var config = new MessageConfiguration(sp)
            .Register<TestRequestHandler>()
            .Register<TestMessageHandler>();
        _sut = new Messenger.Messenger(config);
    }

    [Fact]
    public async Task RequestTest()
    {
        var response = await _sut.Send(new TestRequest()
        {
            In = 6
        });

        Assert.Equal(7, response.Out);
    }

    [Fact]
    public async Task MessageTest()
    {
        TestMessageHandler.Called = false;
        await _sut.Send(new TestMessage());
        Assert.True(TestMessageHandler.Called);
    }
}

public class TestRequest : IRequest<TestResponse>
{
    public int In { get; set; }
}

public class TestResponse
{
    public int Out { get; set; }
}

public class TestRequestHandler : IHandler<TestRequest, TestResponse>
{
    public Task<TestResponse> Handle(TestRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new TestResponse
        {
            Out = request.In + 1
        });
    }
}

public class TestMessage : IMessage
{

}

public class TestMessageHandler : IHandler<TestMessage>
{
    public static bool Called { get; set; }
    public async Task Handle(TestMessage request, CancellationToken cancellationToken)
    {
        Called = true;
    }
}