using MembershipSystem.Domain;

namespace MembershipSystem.Seed;

/// <summary>
/// Members across both seeded branches, playing sports from their own
/// branch only (Member.AssignSport enforces this invariant — a member
/// can never be assigned a sport from a different branch).
/// </summary>
public static class MemberSeedData
{
    public static IReadOnlyList<Member> BuildAll()
    {
        var ada = new Member(MemberId.New(), BranchSeedData.NorthAmsterdam.Id, "Ada", "Lovelace");
        ada.AssignSport(SportSeedData.NorthTennis);
        ada.AssignSport(SportSeedData.NorthSquash);

        var grace = new Member(MemberId.New(), BranchSeedData.NorthAmsterdam.Id, "Grace", "Hopper");
        grace.AssignSport(SportSeedData.NorthFootball);

        var alan = new Member(MemberId.New(), BranchSeedData.SouthAmsterdam.Id, "Alan", "Turing");
        alan.AssignSport(SportSeedData.SouthTennis);

        var margaret = new Member(MemberId.New(), BranchSeedData.SouthAmsterdam.Id, "Margaret", "Hamilton");
        // No sport yet — demonstrates a member is valid with an empty sport set.

        return [ada, grace, alan, margaret];
    }
}
