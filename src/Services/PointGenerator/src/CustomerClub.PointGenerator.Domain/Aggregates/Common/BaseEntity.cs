namespace CustomerClub.PointGenerator.Domain.Aggregates.Common;

public abstract class BaseEntity<TKey>
{
    public TKey Id { get; set; } = default!;
}
