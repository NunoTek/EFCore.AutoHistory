using EFCore.AutoHistory.Extensions;
using Microsoft.EntityFrameworkCore;

namespace EFCore.AutoHistory;

public abstract class AutoHistoryContext : DbContext
{
    public override int SaveChanges() => SaveChangesAsync().Result;

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        this.EnsureAutoHistory();

        int result = await base.SaveChangesAsync(cancellationToken);

        this.CompleteAutoHistory();

        return result;
    }
}
