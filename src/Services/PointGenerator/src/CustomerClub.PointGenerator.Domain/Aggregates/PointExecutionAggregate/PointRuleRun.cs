using CustomerClub.PointGenerator.Domain.Aggregates.PointRulAggregate;

namespace CustomerClub.PointGenerator.Domain.Aggregates.PointExecutionAggregate;

public class PointRuleRun : BaseEntity<int>
{
    public int PointRuleId { get; set; }

    public PointRuleRunTriggerType TriggerType { get; set; }
    public PointRuleRunStatus Status { get; set; } = PointRuleRunStatus.Running;

    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }

    public int TotalRead { get; set; }
    public int TotalProduced { get; set; }
    public int TotalFailed { get; set; }

    public string? ErrorSummary { get; set; }

    public PointRule PointRule { get; set; } = null!;
    public ICollection<PointProducedEvent> ProducedEvents { get; set; } = [];
    public ICollection<PointRunError> Errors { get; set; } = [];
}