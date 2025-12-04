# EFCore.AutoHistory

[![NuGet](https://img.shields.io/nuget/v/EFCore.AutoHistory.svg)](https://www.nuget.org/packages/EFCore.AutoHistory/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)

EFCore.AutoHistory is a powerful .NET library that automatically tracks and records data changes in your Entity Framework Core applications. It provides a seamless way to maintain an audit trail of all modifications made to your entities.

## Features

- 🔄 **Automatic tracking** of entity changes (Added, Modified, Deleted)
- 📝 **Detailed change history** with before/after values stored as JSON
- ⚡ **Easy integration** with existing EF Core applications
- 🎯 **Selective tracking** using `[IncludeHistory]` and `[ExcludeHistory]` attributes
- 🔍 **Fluent query extensions** for easy history retrieval
- ⚙️ **Configurable options** for error handling, logging, and behavior
- 🔧 **Extensible logging** with custom logger support
- 📊 **Composite key support** for complex entity relationships

## Requirements

- .NET 10.0 or later
- Entity Framework Core 10.0 or later

## Installation

You can install the package via NuGet Package Manager:

```bash
dotnet add package EFCore.AutoHistory
```

Or via the Package Manager Console:

```powershell
Install-Package EFCore.AutoHistory
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
- `Created`: Timestamp of the change (UTC by default)

### Using Query Extensions

The library provides fluent query extensions to easily retrieve and filter history records:

```csharp
using EFCore.AutoHistory.Extensions;
using EFCore.AutoHistory.Models;

// Get history for a specific entity
var productHistory = await context.Set<AutoHistory>()
    .ForEntity<Product>(productId)
    .MostRecentFirst()
    .ToListAsync();

// Get all modifications from the last 7 days
var recentChanges = await context.Set<AutoHistory>()
    .Modifications()
    .FromLastDays(7)
    .ToListAsync();

// Get deletions within a date range
var deletions = await context.Set<AutoHistory>()
    .Deletions()
    .InDateRange(startDate, endDate)
    .MostRecentFirst()
    .ToListAsync();

// Get history for a specific table
var orderHistory = await context.Set<AutoHistory>()
    .ForTable("Order")
    .OldestFirst()
    .Take(100)
    .ToListAsync();

// Parse changes to see what was modified
var history = await context.Set<AutoHistory>().FirstAsync();
var changes = history.ParseChangesAsDictionary();
Console.WriteLine($"Before: {changes?.Before}");
Console.WriteLine($"After: {changes?.After}");

// Get list of changed property names
var changedProperties = history.GetChangedPropertyNames();
```

### Available Query Extension Methods

| Method | Description |
|--------|-------------|
| `ForEntity<T>(id)` | Filter by entity type and ID |
| `ForTable(name)` | Filter by table name |
| `InDateRange(start, end)` | Filter by date range |
| `FromLastDays(days)` | Filter records from last N days |
| `OfKind(state)` | Filter by EntityState |
| `Additions()` | Get only Added records |
| `Modifications()` | Get only Modified records |
| `Deletions()` | Get only Deleted records |
| `MostRecentFirst()` | Order by Created descending |
| `OldestFirst()` | Order by Created ascending |
| `ParseChanges<T>()` | Parse JSON to typed object |
| `ParseChangesAsDictionary()` | Parse JSON to dictionary |
| `GetChangedPropertyNames()` | Get list of changed properties |

## How It Works

When you save changes to your database context:

1. The library automatically captures changes before they are saved
2. For modified entities, it records both the original and new values
3. For deleted entities, it stores the last state before deletion
4. For added entities, it stores the initial state
5. All changes are serialized and stored in the AutoHistory table

## Best Practices

### ✅ Do's

1. **Only mark entities that need auditing** with `[IncludeHistory]`
2. **Use `[ExcludeHistory]`** for sensitive fields (passwords, tokens, PII)
3. **Add database indexes** on `TableName`, `RowId`, and `Created` columns
4. **Implement data retention policies** - archive or delete old history records
5. **Use UTC timestamps** for consistency across time zones
6. **Test history tracking** in your integration tests

### ❌ Don'ts

1. **Never track passwords, API keys, or tokens** - always use `[ExcludeHistory]`
2. **Avoid tracking high-frequency updated fields** like `LastAccessTime` or `ViewCount`
3. **Don't track large binary data** - exclude blob/image properties
4. **Avoid circular references** in tracked entities

### Performance Optimization

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    // Add indexes for better query performance
    modelBuilder.Entity<AutoHistory>(entity =>
    {
        entity.HasIndex(e => e.TableName);
        entity.HasIndex(e => e.RowId);
        entity.HasIndex(e => e.Created);
        entity.HasIndex(e => new { e.TableName, e.RowId });
    });
}
```

### Data Retention Example

```csharp
// Archive history older than 90 days
public async Task ArchiveOldHistoryAsync(int daysToKeep = 90)
{
    var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
    
    var oldRecords = await _context.Set<AutoHistory>()
        .Where(h => h.Created < cutoffDate)
        .ToListAsync();
    
    // Move to archive table or delete
    _context.Set<AutoHistory>().RemoveRange(oldRecords);
    await _context.SaveChangesAsync();
}
```

## Troubleshooting

### History is not being recorded

- ✅ Ensure your entity is decorated with `[IncludeHistory]` attribute
- ✅ Verify that `SaveChanges()` or `SaveChangesAsync()` is being called
- ✅ Check that you're either inheriting from `AutoHistoryContext` or calling `EnsureAutoHistory()` and `CompleteAutoHistory()`
- ✅ Confirm the entity has a primary key defined

### Performance issues

- ✅ Add indexes to the AutoHistory table (see Performance Optimization above)
- ✅ Use `[ExcludeHistory]` on large or frequently changed properties
- ✅ Implement an archival strategy for old history records
- ✅ Consider async operations for bulk changes

### JSON parsing errors

- ✅ Ensure the `Changed` column has sufficient length (use `nvarchar(max)`)
- ✅ Check for circular references in navigation properties
- ✅ Verify JSON serialization settings are consistent

### Common Error Messages

| Error | Solution |
|-------|----------|
| "EnsureAutoHistory only support Added, Modified and Deleted entity" | Entity state is Detached or Unchanged |
| Empty history records | Check `[IncludeHistory]` attribute is applied |
| Truncated JSON | Increase `MaxChangedLength` in options |

## API Reference

### Attributes

| Attribute | Target | Description |
|-----------|--------|-------------|
| `[IncludeHistory]` | Class | Marks an entity for history tracking |
| `[ExcludeHistory]` | Property | Excludes a property from tracking |

### AutoHistory Model Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `int` | Primary key |
| `RowId` | `string` | Primary key of tracked entity |
| `TableName` | `string` | Entity type name |
| `Changed` | `string?` | JSON with before/after values |
| `Kind` | `EntityState` | Added, Modified, or Deleted |
| `Created` | `DateTime` | Timestamp (UTC) |

### AutoHistoryChanges Model

```csharp
public class AutoHistoryChanges<TEntity>
{
    public TEntity? Before { get; set; }  // Original values
    public TEntity? After { get; set; }   // New values
}
```

## Migration Guide

### Setting Up the AutoHistory Table

If you're using EF Core migrations, the AutoHistory table will be created automatically. For manual setup:

```sql
CREATE TABLE AutoHistory (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    RowId NVARCHAR(50) NOT NULL,
    TableName NVARCHAR(128) NOT NULL,
    Changed NVARCHAR(MAX),
    Kind INT NOT NULL,
    Created DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    INDEX IX_AutoHistory_TableName (TableName),
    INDEX IX_AutoHistory_RowId (RowId),
    INDEX IX_AutoHistory_Created (Created)
);
```

## Comparison with Alternatives

| Feature | EFCore.AutoHistory | Manual Auditing | Temporal Tables |
|---------|-------------------|-----------------|-----------------|
| Setup Complexity | Low | High | Medium |
| Before/After Values | ✅ | Manual | ✅ |
| Selective Tracking | ✅ | ✅ | ❌ |
| Cross-Database Support | ✅ | ✅ | SQL Server only |
| Query Extensions | ✅ | ❌ | ❌ |
| Performance Impact | Low | Varies | Very Low |

## Contributing

Contributions are welcome! Here's how you can help:

### Reporting Issues

When reporting issues, please include:
- .NET version
- EF Core version
- Database provider
- Minimal code to reproduce the issue
- Expected vs actual behavior

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Inspired by the need for simple, effective audit trails in EF Core applications
- Thanks to all contributors who have helped improve this library

## Support

- 📖 [Documentation](https://github.com/NunoTek/EFCore.AutoHistory#readme)
- 🐛 [Issue Tracker](https://github.com/NunoTek/EFCore.AutoHistory/issues)
- 💬 [Discussions](https://github.com/NunoTek/EFCore.AutoHistory/discussions)

If you find this library helpful, please consider giving it a ⭐ on GitHub!