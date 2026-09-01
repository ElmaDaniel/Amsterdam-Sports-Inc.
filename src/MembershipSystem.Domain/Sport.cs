namespace MembershipSystem.Domain;

public sealed class Sport
{
    public SportId Id { get; }
    public BranchId BranchId { get; }
    public string Name { get; }

    public Sport(SportId id, BranchId branchId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        Id = id;
        BranchId = branchId;
        Name = name;
    }
}
