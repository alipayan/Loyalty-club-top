using CustomerClub.PointGenerator.Contracts;

namespace CustomerClub.PointGenerator.Application;

public sealed class SampleCommandHandler
{
    public Task HandleAsync(CreateSampleCommand command, CancellationToken cancellationToken)
    {
        var @event = new SampleCreatedV1(
            EventId: Guid.NewGuid(),
            EventType: "sample.created.v1",
            EventVersion: "v1",
            OccurredOnUtc: DateTimeOffset.UtcNow,
            CorrelationId: null,
            CausationId: null,
            Producer: "PointGenerator",
            TenantOrClubId: null,
            Name: command.Name);

        return Task.CompletedTask;
    }
}
