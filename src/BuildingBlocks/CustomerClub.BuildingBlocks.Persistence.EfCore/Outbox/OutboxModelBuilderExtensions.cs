namespace CustomerClub.BuildingBlocks.Persistence.EfCore.Outbox;

public static class OutboxModelBuilderExtensions
{
    public static ModelBuilder AddCustomerClubOutbox(
        this ModelBuilder modelBuilder,
        string tableName = "OutboxMessages",
        string? schema = null)
    {
        var entity = modelBuilder.Entity<OutboxMessage>();

        entity.ToTable(tableName, schema);

        entity.HasKey(message => message.Id);

        entity.Property(message => message.EventType)
            .HasMaxLength(300)
            .IsRequired();

        entity.Property(message => message.EventVersion)
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(message => message.Payload)
            .IsRequired();

        entity.Property(message => message.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(message => message.Producer)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(message => message.CorrelationId)
            .HasMaxLength(100);

        entity.Property(message => message.CausationId)
            .HasMaxLength(100);

        entity.Property(message => message.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(message => message.LastError)
            .HasMaxLength(4000);

        entity.HasIndex(message => new
        {
            message.Status,
            message.OccurredOn
        });

        entity.HasIndex(message => message.CorrelationId);

        entity.HasIndex(message => message.ProcessingExpiresOn);

        entity.HasIndex(message => new
        {
            message.EventType,
            message.EventVersion
        });

        return modelBuilder;
    }
}