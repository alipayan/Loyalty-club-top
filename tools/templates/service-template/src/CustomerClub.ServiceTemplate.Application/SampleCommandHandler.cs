using CustomerClub.ServiceTemplate.Contracts;

namespace CustomerClub.ServiceTemplate.Application;

public sealed class SampleCommandHandler
{
    public Task<IResult> HandleAsync(CreateSampleCommand command, CancellationToken cancellationToken)
    {
        var @event = new SampleCreatedV1(
            EventId: Guid.NewGuid(),
            EventType: "sample.created.v1",
            EventVersion: "v1",
            OccurredOnUtc: DateTimeOffset.UtcNow,
            CorrelationId: null,
            CausationId: null,
            Producer: "ServiceTemplate",
            TenantOrClubId: null,
            Name: command.Name);

        return Task.FromResult(Results.Accepted($"/api/sample/{@event.EventId}", @event));
    }
}
