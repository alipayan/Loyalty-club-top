namespace CustomerClub.PointGenerator.Domain.Aggregates.PointRulAggregate;

public class PointRuleSchedule : BaseEntity<int>
{
    public int PointRuleId { get; set; }

    public int? IntervalSeconds { get; set; }
    public string? CronExpr { get; set; }
    public DateTime? LastRunAt { get; set; }
    public DateTime? NextRunAt { get; set; }
    public bool IsEnabled { get; set; }

    public PointRule PointRule { get; set; } = null!;
}