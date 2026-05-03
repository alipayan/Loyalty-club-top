using CustomerClub.PointGenerator.Domain.Aggregates.PointRulAggregate;

namespace CustomerClub.PointGenerator.Domain.Aggregates.DataSourceAggregate;

public class Dataset : BaseEntity<int>
{
    public int DataSourceId { get; set; }
    public DatasetKind Kind { get; set; }
    public string Identifier { get; set; } = null!; // e.g. table name, file path, API endpoint
    public DateTime CreatedAt { get; set; }

    public DataSource DataSource { get; set; } = null!;
    public ICollection<PointRule> PointRules { get; set; } = [];
}