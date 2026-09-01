namespace MembershipSystem.Domain;

public readonly record struct BranchId(Guid Value)
{
    public static BranchId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
