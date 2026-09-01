namespace MembershipSystem.Domain;

public sealed class Member
{
    private readonly HashSet<SportId> _sportIds = [];

    public MemberId Id { get; }
    public BranchId BranchId { get; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string? PhotoPath { get; private set; }
    public IReadOnlySet<SportId> SportIds => _sportIds;

    public Member(MemberId id, BranchId branchId, string firstName, string lastName)
    {
        ValidateFirstName(firstName);
        ValidateLastName(lastName);

        Id = id;
        BranchId = branchId;
        FirstName = firstName;
        LastName = lastName;
    }

    public void Rename(string firstName, string lastName)
    {
        ValidateFirstName(firstName);
        ValidateLastName(lastName);

        FirstName = firstName;
        LastName = lastName;
    }

    public void AssignSport(Sport sport)
    {
        if (sport.BranchId != BranchId)
        {
            throw new InvalidOperationException(
                $"Sport '{sport.Name}' belongs to a different branch than this member.");
        }

        _sportIds.Add(sport.Id);
    }

    public void RemoveSport(SportId sportId)
    {
        _sportIds.Remove(sportId);
    }

    public void SetPhotoPath(string? photoPath)
    {
        PhotoPath = photoPath;
    }

    private static void ValidateFirstName(string firstName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException("FirstName is required.", nameof(firstName));
        }
    }

    private static void ValidateLastName(string lastName)
    {
        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException("LastName is required.", nameof(lastName));
        }
    }
}
