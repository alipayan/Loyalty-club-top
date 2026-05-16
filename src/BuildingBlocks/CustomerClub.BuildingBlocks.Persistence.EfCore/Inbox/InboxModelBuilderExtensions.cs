namespace CustomerClub.BuildingBlocks.Persistence.EfCore.Inbox;

public static class InboxModelBuilderExtensions
{
    public static ModelBuilder AddCustomerClubInbox(
        this ModelBuilder modelBuilder,
        string tableName = "InboxMessages",
        string? schema = null)
    {
        var entity = modelBuilder.Entity<InboxMessage>();

        entity.ToTable(tableName, schema);

        entity.HasKey(message => message.Id);

        entity.Property(message => message.EventType)
            .HasMaxLength(300)
            .IsRequired();

        entity.Property(message => message.Consumer)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(message => message.CorrelationId)
            .HasMaxLength(100);

        entity.HasIndex(message => new
        {
            message.EventId,
            message.Consumer
        }).IsUnique();

        entity.HasIndex(message => message.ProcessedOn);

        return modelBuilder;
    }
}