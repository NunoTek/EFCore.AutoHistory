# EFCore.AutoHistory

EFCore.AutoHistory is a powerful .NET library that automatically tracks and records data changes in your Entity Framework Core applications. It provides a seamless way to maintain an audit trail of all modifications made to your entities.

## Features

- 🔄 Automatic tracking of entity changes (Added, Modified, Deleted)
- 📝 Detailed change history with before/after values
- ⚡ Easy integration with existing EF Core applications
- 🎯 Selective tracking using attributes
- 🔍 Comprehensive change information storage

## Installation

You can install the package via NuGet Package Manager:

```bash
dotnet add package EFCore.AutoHistory
```

## Usage

There are two ways to implement automatic history tracking in your application:

### Option 1: Inherit from AutoHistoryContext

The simplest way to enable automatic history tracking is to make your DbContext inherit from `AutoHistoryContext`:

```csharp
public class YourDbContext : AutoHistoryContext
{
    public DbSet<YourEntity> YourEntities { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Your model configurations...
    }
}
```

### Option 2: Manual Implementation

If you can't inherit from `AutoHistoryContext` (for example, if you're already inheriting from another context), you can manually implement the history tracking:

```csharp
public class YourDbContext : DbContext // or your custom context
{
    public DbSet<YourEntity> YourEntities { get; set; }
    
    public override int SaveChanges()
    {
        this.EnsureAutoHistory();
        var result = base.SaveChanges();
        this.CompleteAutoHistory();
        return result;
    }
    
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        this.EnsureAutoHistory();
        var result = await base.SaveChangesAsync(cancellationToken);
        this.CompleteAutoHistory();
        return result;
    }
}
```

### Mark Entities for History Tracking

Use the `[IncludeHistory]` attribute to specify which entities should be tracked:

```csharp
[IncludeHistory]
public class YourEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    
    [ExcludeHistory]
    public string TemporaryField { get; set; } // This property will not be tracked
}
```

### Accessing History Data

The history records are stored in the `AutoHistory` table. Each record contains:

- `Id`: Primary key
- `RowId`: The primary key of the changed entity
- `TableName`: Name of the entity's table
- `Changed`: JSON string containing the before/after values
- `Kind`: Type of change (Added/Modified/Deleted)
- `Created`: Timestamp of the change

## How It Works

When you save changes to your database context:

1. The library automatically captures changes before they are saved
2. For modified entities, it records both the original and new values
3. For deleted entities, it stores the last state before deletion
4. For added entities, it stores the initial state
5. All changes are serialized and stored in the AutoHistory table

## Best Practices

1. Only mark entities that need auditing with `[IncludeHistory]`
2. Use `[ExcludeHistory]` for sensitive or unnecessary fields
3. Consider the performance impact when tracking large entities
4. Regularly archive old history records if needed

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

If you encounter any issues or have questions, please file them in the GitHub issues section. 