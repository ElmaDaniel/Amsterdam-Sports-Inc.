using MembershipSystem.Domain;
using MembershipSystem.UseCases.Ports;
using Microsoft.EntityFrameworkCore;

namespace MembershipSystem.Adapters;

public sealed class EfBranchRepository(MembershipDbContext context) : IBranchRepository
{
    public Task<Branch?> GetById(BranchId branchId) =>
        context.Branches.FirstOrDefaultAsync(b => b.Id == branchId);

    public async Task<IReadOnlyList<Branch>> ListAll() =>
        await context.Branches.ToListAsync();

    public async Task Add(Branch branch)
    {
        context.Branches.Add(branch);
        await context.SaveChangesAsync();
    }
}
