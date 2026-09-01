using MembershipSystem.Domain;

namespace MembershipSystem.Seed;

/// <summary>
/// Sports across both seeded branches. Deliberately gives North
/// Amsterdam and South Amsterdam their own distinct Sport rows for the
/// same names (Tennis, Squash) to demonstrate the per-branch scoping
/// from Decision 3 — these are not shared rows.
/// </summary>
public static class SportSeedData
{
    public static readonly Sport NorthTennis = new(SportId.New(), BranchSeedData.NorthAmsterdam.Id, "Tennis");
    public static readonly Sport NorthSquash = new(SportId.New(), BranchSeedData.NorthAmsterdam.Id, "Squash");
    public static readonly Sport NorthFootball = new(SportId.New(), BranchSeedData.NorthAmsterdam.Id, "Football");

    public static readonly Sport SouthTennis = new(SportId.New(), BranchSeedData.SouthAmsterdam.Id, "Tennis");
    public static readonly Sport SouthSquash = new(SportId.New(), BranchSeedData.SouthAmsterdam.Id, "Squash");

    public static IReadOnlyList<Sport> All =>
        [NorthTennis, NorthSquash, NorthFootball, SouthTennis, SouthSquash];
}
