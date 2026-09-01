using MembershipSystem.Domain;

namespace MembershipSystem.Seed;

/// <summary>
/// The two branches every other seed file (sports, members) is defined
/// against. Names/IDs are fixed here so SportSeedData and MemberSeedData
/// can reference them directly instead of looking them up.
/// </summary>
public static class BranchSeedData
{
    public static readonly Branch NorthAmsterdam = new(BranchId.New(), "North Amsterdam");
    public static readonly Branch SouthAmsterdam = new(BranchId.New(), "South Amsterdam");

    public static IReadOnlyList<Branch> All => [NorthAmsterdam, SouthAmsterdam];
}
