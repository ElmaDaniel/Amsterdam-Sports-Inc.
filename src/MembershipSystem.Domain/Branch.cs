namespace MembershipSystem.Domain;

public sealed class Branch
{
    public BranchId Id { get; }
    public string Name { get; }

    public Branch(BranchId id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        Id = id;
        Name = name;
    }
}
