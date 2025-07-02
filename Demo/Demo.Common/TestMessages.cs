using Messenger;
using Microsoft.Extensions.Logging;

namespace Demo.Common;

public record TestMessage(string Message) : IMessage;

public class TestMessageHandler : IHandler<TestMessage>
{
    private readonly ILogger<TestMessageHandler> _logger;

    public TestMessageHandler(ILogger<TestMessageHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(TestMessage request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(request.Message);
        return Task.CompletedTask;
    }
}

public record AddResponse(int Result);
public record AddRequest(int A, int B) : IRequest<AddResponse>;
public class AddHandler : IHandler<AddRequest, AddResponse>
{
    private readonly ILogger<AddHandler> _logger;

    public AddHandler(ILogger<AddHandler> logger)
    {
        _logger = logger;
    }

    public Task<AddResponse> Handle(AddRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("AddHandler Got Called");
        return Task.FromResult(new AddResponse(request.A + request.B));
    }
}

public record StringLowerResponse(string Result);
public record StringLowerRequest(string S) : IRequest<StringLowerResponse>;
public class StringHandler : IHandler<StringLowerRequest, StringLowerResponse>
{
    private readonly ILogger<StringHandler> _logger;

    public StringHandler(ILogger<StringHandler> logger)
    {
        _logger = logger;
    }

    public Task<StringLowerResponse> Handle(StringLowerRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("StringHandler Got Called");
        return Task.FromResult(new StringLowerResponse(request.S.ToLowerInvariant()));
    }
}

public class MessagePacket
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public required string Data { get; set; }
}