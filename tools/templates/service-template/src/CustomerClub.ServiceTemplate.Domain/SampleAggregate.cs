namespace CustomerClub.ServiceTemplate.Domain;

public sealed class SampleAggregate
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Sample name cannot be empty.", nameof(newName));

        Name = newName;
    }
}
