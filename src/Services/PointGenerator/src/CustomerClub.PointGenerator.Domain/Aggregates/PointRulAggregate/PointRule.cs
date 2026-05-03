using CustomerClub.PointGenerator.Domain.Aggregates.DataSourceAggregate;
using CustomerClub.PointGenerator.Domain.Aggregates.PointExecutionAggregate;

namespace CustomerClub.PointGenerator.Domain.Aggregates.PointRulAggregate;

public class PointRule : AuditBaseEntity<int>
{
    public int DatasetId { get; set; }
    public long WalletTypeId { get; set; }

    public string Name { get; set; } = null!;
    public string FormulaDsl { get; set; } = null!;
    public bool IsEnabled { get; set; } = true;

    public Dataset Dataset { get; set; } = null!;

    // چون WalletType توی schema دیگه است، فعلاً navigation نذار مگر entityش داخل همین context باشد
    public PointRuleSchedule? Schedule { get; set; }
    public PointRuleCheckpoint? Checkpoint { get; set; }

    public ICollection<PointRuleRun> Runs { get; set; } = [];
}