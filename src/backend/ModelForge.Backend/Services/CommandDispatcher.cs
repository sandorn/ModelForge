using System.Collections.Concurrent;
using ModelForge.Contracts;

namespace ModelForge.Backend.Services;

public interface ICommandDispatcher
{
    Task<CommandDispatchResponse> DispatchAsync(CommandDispatchRequest request, CancellationToken cancellationToken);
}

public sealed class InMemoryCommandDispatcher : ICommandDispatcher
{
    private readonly ICommandCatalog _catalog;
    private readonly ConcurrentQueue<CommandDispatchResponse> _dispatchLog = new();

    public InMemoryCommandDispatcher(ICommandCatalog catalog)
    {
        _catalog = catalog;
    }

    public Task<CommandDispatchResponse> DispatchAsync(CommandDispatchRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var command = _catalog.FindById(request.CommandId);
        var response = new CommandDispatchResponse
        {
            DispatchId = Guid.NewGuid().ToString("N"),
            CommandId = request.CommandId,
            Status = command is null ? CommandStatus.Failed : CommandStatus.Accepted,
            Message = command is null
                ? $"未知命令：{request.CommandId}"
                : $"命令已由后端桥接接收，目标执行端：{command.Target}"
        };

        _dispatchLog.Enqueue(response);
        return Task.FromResult(response);
    }
}
