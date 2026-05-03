namespace CustomerClub.PointGenerator.Domain.Aggregates.FileAggregate;

public class UploadedFile : BaseEntity<int>
{
    public string OriginalFileName { get; set; } = null!;
    public string StoredFileName { get; set; } = null!;
    public string StoragePath { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public string Extension { get; set; } = null!;
    public long SizeInBytes { get; set; }

    public DateTime CreatedAt { get; set; }
}