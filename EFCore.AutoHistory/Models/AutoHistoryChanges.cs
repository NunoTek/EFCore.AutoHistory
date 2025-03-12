namespace EFCore.AutoHistory.Models;

public class AutoHistoryChanges<TEntity>
{
    public TEntity? Before { get; set; }
    public TEntity? After { get; set; }
}