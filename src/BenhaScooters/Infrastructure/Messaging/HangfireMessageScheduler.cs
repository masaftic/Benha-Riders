using BenhaScooters.Application.Workflows;
using Hangfire;

namespace BenhaScooters.Infrastructure.Messaging;

public class HangfireMessageScheduler(IMessageDispatcher dispatcher) : IMessageScheduler
{
    public Task EnqueueAsync<TMessage>(TMessage message, CancellationToken ct = default)
    {
        BackgroundJob.Enqueue(() => dispatcher.Dispatch(message, ct));
        return Task.CompletedTask;
    }

    public Task ScheduleAsync<TMessage>(TMessage message, TimeSpan delay, CancellationToken ct = default)
    {
        BackgroundJob.Schedule(() => dispatcher.Dispatch(message, ct), delay);
        return Task.CompletedTask;
    }
}
