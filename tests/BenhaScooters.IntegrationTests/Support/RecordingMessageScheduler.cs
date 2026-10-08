using BenhaScooters.Application.Workflows;

namespace BenhaScooters.IntegrationTests.Support;

public sealed record RecordedWorkflowMessage(object Message, TimeSpan? Delay);

public sealed class RecordingMessageScheduler : IMessageScheduler
{
    private readonly List<RecordedWorkflowMessage> _messages = [];

    public IReadOnlyList<RecordedWorkflowMessage> Messages => _messages.AsReadOnly();

    public Task EnqueueAsync<TMessage>(TMessage message, CancellationToken ct = default)
    {
        _messages.Add(new RecordedWorkflowMessage(message!, null));
        return Task.CompletedTask;
    }

    public Task ScheduleAsync<TMessage>(TMessage message, TimeSpan delay, CancellationToken ct = default)
    {
        _messages.Add(new RecordedWorkflowMessage(message!, delay));
        return Task.CompletedTask;
    }

    public async Task DrainImmediateAsync(MediatR.ISender sender, CancellationToken ct = default)
    {
        while (true)
        {
            var nextIndex = _messages.FindIndex(m => m.Delay == null);
            if (nextIndex < 0)
                break;

            var next = _messages[nextIndex];
            _messages.RemoveAt(nextIndex);

            if (next.Message is MediatR.IBaseRequest request)
            {
                await sender.Send(request, ct);
            }
        }
    }

    public void Clear() => _messages.Clear();
}
