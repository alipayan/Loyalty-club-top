namespace CustomerClub.PointGenerator.Domain.Aggregates.PointExecutionAggregate;

public class PointRunError : BaseEntity<int>
{
    public long RunId { get; set; }

    public string? ExternalKey { get; set; }
    public string? ErrorCode { get; set; }
    public string ErrorMessage { get; set; } = null!;
    public string? RowSnapshotJson { get; set; }
    public DateTime CreatedAt { get; set; }

    public PointRuleRun Run { get; set; } = null!;
}