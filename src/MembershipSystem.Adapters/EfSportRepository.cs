using MembershipSystem.Domain;
using MembershipSystem.UseCases.Ports;
using Microsoft.EntityFrameworkCore;

namespace MembershipSystem.Adapters;

public sealed class EfSportRepository(MembershipDbContext context) : ISportRepository
{
    public async Task<IReadOnlyList<Sport>> ListByBranch(BranchId branchId) =>
        await context.Sports.Where(s => s.BranchId == branchId).ToListAsync();

    public Task<Sport?> GetById(BranchId branchId, SportId sportId) =>
        context.Sports.FirstOrDefaultAsync(s => s.Id == sportId && s.BranchId == branchId);
}
