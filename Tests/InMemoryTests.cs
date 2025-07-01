using Messenger;
using Microsoft.Extensions.DependencyInjection;

namespace Tests;

public class InMemoryTests
{
    private readonly Messenger.ISender _sut;
    private readonly TestState _testState;

    public InMemoryTests()
    {
        _testState = new TestState();
        var sp = new ServiceCollection()
            .AddMessenger(o => o.Register<TestRequestHandler>().Register<TestMessageHandler>())
            .AddSingleton(_testState)
            .BuildServiceProvider();

        _sut = sp.GetRequiredService<ISender>();
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
        _testState.Called = false;
        await _sut.Send(new TestMessage());
        Assert.True(_testState.Called);
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
    private readonly TestState _testState;

    public TestMessageHandler(TestState testState)
    {
        _testState = testState;
    }

    public Task Handle(TestMessage request, CancellationToken cancellationToken)
    {
        _testState.Called = true;
        return Task.CompletedTask;
    }
}

public class TestState
{
    public bool Called { get; set; }
}