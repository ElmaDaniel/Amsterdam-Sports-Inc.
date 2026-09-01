using MembershipSystem.Domain;
using Microsoft.EntityFrameworkCore;

namespace MembershipSystem.Adapters;

public sealed class MembershipDbContext(DbContextOptions<MembershipDbContext> options) : DbContext(options)
{
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Sport> Sports => Set<Sport>();
    public DbSet<Member> Members => Set<Member>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Branch>(branch =>
        {
            branch.HasKey(b => b.Id);
            branch.Property(b => b.Id)
                .HasConversion(id => id.Value, value => new BranchId(value));
            branch.Property(b => b.Name).IsRequired();
        });

        modelBuilder.Entity<Sport>(sport =>
        {
            sport.HasKey(s => s.Id);
            sport.Property(s => s.Id)
                .HasConversion(id => id.Value, value => new SportId(value));
            sport.Property(s => s.BranchId)
                .HasConversion(id => id.Value, value => new BranchId(value));
            sport.Property(s => s.Name).IsRequired();
            sport.HasIndex(s => new { s.BranchId, s.Name });
        });

        modelBuilder.Entity<Member>(member =>
        {
            member.HasKey(m => m.Id);
            member.Property(m => m.Id)
                .HasConversion(id => id.Value, value => new MemberId(value));
            member.Property(m => m.BranchId)
                .HasConversion(id => id.Value, value => new BranchId(value));
            member.Property(m => m.FirstName).IsRequired();
            member.Property(m => m.LastName).IsRequired();
            member.Property(m => m.PhotoPath);

            // SportIds is a private HashSet<SportId> backing field, not a
            // navigation — Member only ever needs the set of sport IDs it
            // plays, never a live reference to Sport itself (see layer
            // map). Stored as a single delimited string rather than EF's
            // primitive-collection support, which insists on its own
            // element conversion for non-primitive element types like
            // SportId and conflicts with a whole-collection HasConversion.
            member.Property<HashSet<SportId>>("_sportIds")
                .HasField("_sportIds")
                .UsePropertyAccessMode(PropertyAccessMode.Field)
                .HasConversion(
                    ids => string.Join(',', ids.Select(id => id.Value)),
                    value => string.IsNullOrEmpty(value)
                        ? new HashSet<SportId>()
                        : value.Split(',').Select(v => new SportId(Guid.Parse(v))).ToHashSet())
                .HasColumnName("SportIds")
                .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<HashSet<SportId>>(
                    (a, b) => (a ?? new()).SetEquals(b ?? new()),
                    s => s.Aggregate(0, (hash, id) => HashCode.Combine(hash, id.GetHashCode())),
                    s => new HashSet<SportId>(s)));
        });
    }
}
