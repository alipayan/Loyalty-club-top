using CustomerClub.PointGenerator.Domain.Aggregates.FileAggregate;

namespace CustomerClub.PointGenerator.Domain.Aggregates.DataSourceAggregate;

public class DataSource : AuditBaseEntity<int>
{
    public DataSourceType Type { get; set; }
    public string DisplayName { get; set; } = null!;
    public DataSourceStatus Status { get; set; } = DataSourceStatus.Active;
    public string? SecretRef { get; set; } // store actual secret value like connection string

    // فقط برای File-based sourceها مثل Excel
    public long? UploadedFileId { get; set; }

    public UploadedFile? UploadedFile { get; set; }

    public ICollection<Dataset> Datasets { get; set; } = [];
}