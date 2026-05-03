namespace CustomerClub.PointGenerator.Domain.Aggregates.PointRulAggregate;

public class PointRuleCheckpoint : BaseEntity<int>
{
    public int PointRuleId { get; set; }

    public CheckpointMode Mode { get; set; } = CheckpointMode.None;
    public string? Value { get; set; }
    public DateTime UpdatedAt { get; set; }

    public PointRule PointRule { get; set; } = null!;
}