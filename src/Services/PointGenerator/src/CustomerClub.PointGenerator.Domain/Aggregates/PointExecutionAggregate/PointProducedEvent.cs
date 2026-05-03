namespace CustomerClub.PointGenerator.Domain.Aggregates.PointExecutionAggregate;

public class PointProducedEvent : BaseEntity<int>
{
    public int RunId { get; set; }

    public decimal Points { get; set; }
    public string MemberKey { get; set; } = null!;
    public string ExternalKey { get; set; } = null!;
    public string IdempotencyKey { get; set; } = null!;
    public string PayloadJson { get; set; } = null!;
    public ProducedEventStatus Status { get; set; } = ProducedEventStatus.Pending;
    public DateTime CreatedAt { get; set; }

    public PointRuleRun Run { get; set; } = null!;
}