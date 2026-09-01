using MembershipSystem.Domain;
using MembershipSystem.UseCases.Ports;

namespace MembershipSystem.UseCases;

public sealed class MemberUseCases(
    IMemberRepository members,
    ISportRepository sports,
    IBranchRepository branches,
    IPhotoStore photos)
{
    public async Task<UseCaseResult<IReadOnlyList<MemberSummary>>> ListMembers(BranchId branchId)
    {
        if (await branches.GetById(branchId) is null)
        {
            return UseCaseResult<IReadOnlyList<MemberSummary>>.NotFound();
        }

        var found = await members.ListByBranch(branchId);
        var summaries = new List<MemberSummary>();
        foreach (var member in found)
        {
            var sportNames = await ResolveSportNames(branchId, member.SportIds);
            summaries.Add(new MemberSummary(member.Id.Value, member.FirstName, member.LastName, sportNames));
        }

        return UseCaseResult<IReadOnlyList<MemberSummary>>.Success(summaries);
    }

    public async Task<UseCaseResult<MemberDetail>> GetMember(BranchId branchId, MemberId memberId)
    {
        var member = await members.GetById(branchId, memberId);
        if (member is null)
        {
            return UseCaseResult<MemberDetail>.NotFound();
        }

        return UseCaseResult<MemberDetail>.Success(await ToDetail(branchId, member));
    }

    public async Task<UseCaseResult<MemberDetail>> CreateMember(
        BranchId branchId, string firstName, string lastName, IReadOnlyList<Guid> sportIds)
    {
        if (await branches.GetById(branchId) is null)
        {
            return UseCaseResult<MemberDetail>.NotFound();
        }

        var validation = ValidateNames(firstName, lastName);
        if (validation is not null)
        {
            return UseCaseResult<MemberDetail>.ValidationFailed(validation);
        }

        var resolvedSports = await ResolveSports(branchId, sportIds);
        if (resolvedSports.Error is not null)
        {
            return UseCaseResult<MemberDetail>.ValidationFailed(resolvedSports.Error);
        }

        var member = new Member(MemberId.New(), branchId, firstName, lastName);
        foreach (var sport in resolvedSports.Sports)
        {
            member.AssignSport(sport);
        }

        await members.Add(member);

        return UseCaseResult<MemberDetail>.Success(await ToDetail(branchId, member));
    }

    public async Task<UseCaseResult<MemberDetail>> UpdateMember(
        BranchId branchId, MemberId memberId, string firstName, string lastName, IReadOnlyList<Guid> sportIds)
    {
        var member = await members.GetById(branchId, memberId);
        if (member is null)
        {
            return UseCaseResult<MemberDetail>.NotFound();
        }

        var validation = ValidateNames(firstName, lastName);
        if (validation is not null)
        {
            return UseCaseResult<MemberDetail>.ValidationFailed(validation);
        }

        var resolvedSports = await ResolveSports(branchId, sportIds);
        if (resolvedSports.Error is not null)
        {
            return UseCaseResult<MemberDetail>.ValidationFailed(resolvedSports.Error);
        }

        member.Rename(firstName, lastName);
        foreach (var sportId in member.SportIds.ToList())
        {
            member.RemoveSport(sportId);
        }

        foreach (var sport in resolvedSports.Sports)
        {
            member.AssignSport(sport);
        }

        await members.Update(member);

        return UseCaseResult<MemberDetail>.Success(await ToDetail(branchId, member));
    }

    public async Task<UseCaseResult> RemoveMember(BranchId branchId, MemberId memberId)
    {
        if (await members.GetById(branchId, memberId) is null)
        {
            return UseCaseResult.NotFound();
        }

        await members.Remove(branchId, memberId);

        return UseCaseResult.Success();
    }

    public async Task<UseCaseResult<MemberDetail>> SetMemberPhoto(
        BranchId branchId, MemberId memberId, Stream content, string contentType)
    {
        var member = await members.GetById(branchId, memberId);
        if (member is null)
        {
            return UseCaseResult<MemberDetail>.NotFound();
        }

        var saveResult = await photos.Save(memberId, content, contentType);
        if (!saveResult.IsSuccess)
        {
            return UseCaseResult<MemberDetail>.ValidationFailed(saveResult.Error!);
        }

        member.SetPhotoPath(saveResult.PhotoPath);
        await members.Update(member);

        return UseCaseResult<MemberDetail>.Success(await ToDetail(branchId, member));
    }

    private static string? ValidateNames(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
        {
            return "FirstName is required. LastName is required.";
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            return "FirstName is required.";
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return "LastName is required.";
        }

        return null;
    }

    private async Task<(IReadOnlyList<Sport> Sports, string? Error)> ResolveSports(
        BranchId branchId, IReadOnlyList<Guid> sportIds)
    {
        var resolved = new List<Sport>();
        foreach (var rawId in sportIds)
        {
            var sport = await sports.GetById(branchId, new SportId(rawId));
            if (sport is null)
            {
                return ([], $"Sport '{rawId}' does not exist in this branch.");
            }

            resolved.Add(sport);
        }

        return (resolved, null);
    }

    private async Task<IReadOnlyList<string>> ResolveSportNames(BranchId branchId, IEnumerable<SportId> sportIds)
    {
        var names = new List<string>();
        foreach (var sportId in sportIds)
        {
            var sport = await sports.GetById(branchId, sportId);
            if (sport is not null)
            {
                names.Add(sport.Name);
            }
        }

        return names;
    }

    private async Task<MemberDetail> ToDetail(BranchId branchId, Member member)
    {
        var sportSummaries = new List<SportSummary>();
        foreach (var sportId in member.SportIds)
        {
            var sport = await sports.GetById(branchId, sportId);
            if (sport is not null)
            {
                sportSummaries.Add(new SportSummary(sport.Id.Value, sport.Name));
            }
        }

        return new MemberDetail(member.Id.Value, member.FirstName, member.LastName, member.PhotoPath, sportSummaries);
    }
}
