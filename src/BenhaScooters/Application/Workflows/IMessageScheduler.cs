namespace BenhaScooters.Application.Workflows;

public interface IMessageScheduler
{
    Task EnqueueAsync<TMessage>(TMessage message, CancellationToken ct = default);
    Task ScheduleAsync<TMessage>(TMessage message, TimeSpan delay, CancellationToken ct = default);
}
