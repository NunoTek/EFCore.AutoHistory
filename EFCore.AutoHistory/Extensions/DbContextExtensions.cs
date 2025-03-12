using EFCore.AutoHistory.Attributes;
using EFCore.AutoHistory.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Collections.Concurrent;
using System.Dynamic;

namespace EFCore.AutoHistory.Extensions;

/// <summary>
/// Represents a plugin for Microsoft.EntityFrameworkCore to support automatically recording data changes history.
/// </summary>
public static class DbContextExtensions
{
    internal static ConcurrentQueue<EntityEntry> AddedEntries { get; set; }


    /// <summary>
    /// Ensures the automatic history for modified and deleted entries.
    /// </summary>
    /// <param name="context">The context.</param>
    public static void EnsureAutoHistory(this DbContext context)
    {
        EnsureAutoHistory(context, () => new Models.AutoHistory(), e => e.State == EntityState.Modified || e.State == EntityState.Deleted);


        AddedEntries ??= new ConcurrentQueue<EntityEntry>();

        // Must ToArray() here for excluding the AutoHistory model.
        EntityEntry[] addedEntries = context.ChangeTracker.Entries().Where(e => e.State == EntityState.Added && IncludeHistory(e)).ToArray();
        foreach (EntityEntry entry in addedEntries)
            AddedEntries.Enqueue(entry);
    }

    /// <summary>
    /// Ensures the automatic history for added entries.
    /// </summary>
    /// <param name="context"></param>
    public static void CompleteAutoHistory(this DbContext context)
    {
        if (AddedEntries == null || AddedEntries.Count == 0)
            return;

        var addedEntries = new List<EntityEntry>();
        while (AddedEntries.TryDequeue(out EntityEntry entry))
            addedEntries.Add(entry);

        EnsureEntriesHistory(context, () => new Models.AutoHistory(), addedEntries.ToArray(), false, true);
    }

    internal static TAutoHistory? AutoHistory<TAutoHistory>(
        this EntityEntry entry,
        Func<TAutoHistory> createHistoryFactory,
        bool completeEntry = false,
        bool recentlyAdded = false
    )
    where TAutoHistory : Models.AutoHistory
    {
        if (!IncludeHistory(entry))
            return null;

        IEnumerable<PropertyEntry> properties = ExcludePropertiesHistory(entry);
        string? changed = ChangedProperties(entry, properties.ToArray(), completeEntry, recentlyAdded);

        if (string.IsNullOrEmpty(changed))
            return null;

        TAutoHistory history = createHistoryFactory();
        history.TableName = entry.Entity.GetType().Name; // entry.Metadata.GetTableName();
        history.RowId = entry.PrimaryKey();
        history.Kind = recentlyAdded ? EntityState.Added : entry.State;
        history.Changed = !string.IsNullOrEmpty(changed) ? changed : null;

        return history;
    }

    #region EnsureAutoHistory

    internal static void EnsureAutoHistory<TAutoHistory>(
        this DbContext context,
        Func<TAutoHistory> createHistoryFactory,
        Func<EntityEntry, bool> filter,
        bool completeEntry = false
    )
    where TAutoHistory : Models.AutoHistory
    {
        // Must ToArray() here for excluding the AutoHistory model.
        // Currently, only support Modified and Deleted entity.
        EntityEntry[] entries = context.ChangeTracker.Entries().Where(filter).ToArray();
        EnsureEntriesHistory(context, createHistoryFactory, entries, completeEntry);
    }

    internal static void EnsureEntriesHistory<TAutoHistory>(
         this DbContext context,
         Func<TAutoHistory> createHistoryFactory,
         EntityEntry[] entries,
         bool completeEntry = false,
         bool recentlyAdded = false
    )
    where TAutoHistory : Models.AutoHistory
    {
        foreach (var entry in entries)
        {
            try
            {
                var autoHistory = entry.AutoHistory(createHistoryFactory, completeEntry, recentlyAdded);
                if (autoHistory != null && !string.IsNullOrEmpty(autoHistory.Changed))
                {
                    context.Add<TAutoHistory>(autoHistory);
                }
            }
            catch { }
        }
    }

    #endregion EnsureAutoHistory

    internal static string? ChangedProperties(EntityEntry entry, PropertyEntry[] properties, bool completeEntry = false, bool recentlyAdded = false)
    {
        dynamic result = new ExpandoObject();

        dynamic bef = new ExpandoObject();
        dynamic aft = new ExpandoObject();

        switch (entry.State)
        {
            case EntityState.Modified:
                if (!properties.Any(p => p.IsModified))
                    return null;

                PropertyValues? databaseValues = entry.GetDatabaseValues();

                foreach (PropertyEntry prop in properties.Where(x => x.IsModified))
                {
                    object? originalValue = null;
                    // Get Data
                    if (prop.OriginalValue != null)
                    {
                        if (!prop.OriginalValue.Equals(prop.CurrentValue))
                        {
                            originalValue = prop.OriginalValue;
                        }
                        else
                        {
                            originalValue = databaseValues?.GetValue<object>(prop.Metadata.Name);
                        }
                    }

                    // Compare Datas
                    if (!completeEntry)
                    {
                        var oldValue = originalValue?.ToString() ?? string.Empty;
                        var newValue = prop.CurrentValue?.ToString() ?? string.Empty;

                        switch (prop.Metadata.ClrType.Name)
                        {
                            case nameof(Decimal):
                            case nameof(Double):
                                oldValue = decimal.Parse(oldValue).ToString("0.00");
                                newValue = decimal.Parse(newValue).ToString("0.00");
                                break;
                        }

                        if (oldValue == newValue)
                            continue;
                    }

                    // Prepare Data
                    ((IDictionary<string, object>)bef)[prop.Metadata.Name] = originalValue;
                    ((IDictionary<string, object>)aft)[prop.Metadata.Name] = prop.CurrentValue;
                }
                break;

            case EntityState.Deleted:
                foreach (PropertyEntry prop in properties)
                {
                    ((IDictionary<string, object>)bef)[prop.Metadata.Name] = prop.OriginalValue;
                }
                break;

            case EntityState.Unchanged:
            case EntityState.Added:
                if (!recentlyAdded)
                    throw new NotSupportedException("EnsureAutoHistory only support Added, Modified and Deleted entity.");

                foreach (PropertyEntry prop in properties)
                {
                    ((IDictionary<string, object>)aft)[prop.Metadata.Name] = prop.OriginalValue;
                }
                break;

            case EntityState.Detached:
            default:
                throw new NotSupportedException("EnsureAutoHistory only support Added, Modified and Deleted entity.");
        }


        if (bef == new ExpandoObject() && aft == new ExpandoObject())
            return null;


        ((IDictionary<string, object>)result)[nameof(Models.AutoHistoryChanges<dynamic>.Before)] = bef;
        ((IDictionary<string, object>)result)[nameof(Models.AutoHistoryChanges<dynamic>.After)] = aft;

        return SerializerHelper.Serialize(result);
    }

    internal static string PrimaryKey(this EntityEntry entry)
    {
        if (entry?.Metadata?.FindPrimaryKey() == null)
            return string.Empty;

        IKey key = entry.Metadata.FindPrimaryKey();

        var values = new List<object>();
        foreach (IProperty property in key.Properties)
        {
            object value = entry.Property(property.Name).CurrentValue ?? "0";
            if (value != null)
            {
                values.Add(value);
            }
        }

        return string.Join(",", values);
    }

    #region Reflection

    internal static bool IncludeHistory(EntityEntry entry)
    {
        // Check if include attributes on class entity type
        return entry.Metadata.ClrType.GetCustomAttributes(typeof(IncludeHistoryAttribute), true).FirstOrDefault() is IncludeHistoryAttribute attr;
    }

    internal static IEnumerable<PropertyEntry> ExcludePropertiesHistory(EntityEntry entry)
    {
        // List not excluded mapped properties for the entity type.
        // (include shadow properties, not include navigations & references)
        IEnumerable<string> excludedProperties = entry.Metadata.ClrType.GetProperties()
            .Where(p => p.GetCustomAttributes(typeof(ExcludeHistoryAttribute), true).Count() > 0)
            .Select(p => p.Name);

        return entry.Properties.Where(f => !excludedProperties.Contains(f.Metadata.Name));
    }

    #endregion Reflection
}