using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EFCore.AutoHistory.Extensions;

/// <summary>
/// Extension methods for querying AutoHistory records.
/// </summary>
public static class AutoHistoryQueryExtensions
{
    /// <summary>
    /// Gets history records for a specific entity type by its ID.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="historySet">The AutoHistory DbSet.</param>
    /// <param name="entityId">The entity's primary key value.</param>
    /// <returns>A queryable of history records for the specified entity.</returns>
    public static IQueryable<Models.AutoHistory> ForEntity<TEntity>(
        this DbSet<Models.AutoHistory> historySet,
        object entityId) where TEntity : class
    {
        var tableName = typeof(TEntity).Name;
        var rowId = entityId.ToString()!;
        return historySet.Where(h => h.TableName == tableName && h.RowId == rowId);
    }

    /// <summary>
    /// Gets history records for a specific entity type by its ID.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="query">The AutoHistory queryable.</param>
    /// <param name="entityId">The entity's primary key value.</param>
    /// <returns>A queryable of history records for the specified entity.</returns>
    public static IQueryable<Models.AutoHistory> ForEntity<TEntity>(
        this IQueryable<Models.AutoHistory> query,
        object entityId) where TEntity : class
    {
        var tableName = typeof(TEntity).Name;
        var rowId = entityId.ToString()!;
        return query.Where(h => h.TableName == tableName && h.RowId == rowId);
    }

    /// <summary>
    /// Gets history records for a specific table name.
    /// </summary>
    /// <param name="query">The AutoHistory queryable.</param>
    /// <param name="tableName">The table name.</param>
    /// <returns>A queryable of history records for the specified table.</returns>
    public static IQueryable<Models.AutoHistory> ForTable(
        this IQueryable<Models.AutoHistory> query,
        string tableName)
    {
        return query.Where(h => h.TableName == tableName);
    }

    /// <summary>
    /// Gets history records within a date range.
    /// </summary>
    /// <param name="query">The AutoHistory queryable.</param>
    /// <param name="startDate">The start date (inclusive).</param>
    /// <param name="endDate">The end date (inclusive). If null, includes all records after startDate.</param>
    /// <returns>A queryable of history records within the date range.</returns>
    public static IQueryable<Models.AutoHistory> InDateRange(
        this IQueryable<Models.AutoHistory> query,
        DateTime startDate,
        DateTime? endDate = null)
    {
        query = query.Where(h => h.Created >= startDate);
        if (endDate.HasValue)
            query = query.Where(h => h.Created <= endDate.Value);
        return query;
    }

    /// <summary>
    /// Gets history records from the last specified number of days.
    /// </summary>
    /// <param name="query">The AutoHistory queryable.</param>
    /// <param name="days">The number of days to look back.</param>
    /// <returns>A queryable of history records from the last specified days.</returns>
    public static IQueryable<Models.AutoHistory> FromLastDays(
        this IQueryable<Models.AutoHistory> query,
        int days)
    {
        var startDate = DateTime.UtcNow.AddDays(-days);
        return query.Where(h => h.Created >= startDate);
    }

    /// <summary>
    /// Gets only records of a specific change type.
    /// </summary>
    /// <param name="query">The AutoHistory queryable.</param>
    /// <param name="kind">The entity state (Added, Modified, Deleted).</param>
    /// <returns>A queryable of history records with the specified kind.</returns>
    public static IQueryable<Models.AutoHistory> OfKind(
        this IQueryable<Models.AutoHistory> query,
        EntityState kind)
    {
        return query.Where(h => h.Kind == kind);
    }

    /// <summary>
    /// Gets only Added records.
    /// </summary>
    /// <param name="query">The AutoHistory queryable.</param>
    /// <returns>A queryable of Added history records.</returns>
    public static IQueryable<Models.AutoHistory> Additions(
        this IQueryable<Models.AutoHistory> query)
    {
        return query.OfKind(EntityState.Added);
    }

    /// <summary>
    /// Gets only Modified records.
    /// </summary>
    /// <param name="query">The AutoHistory queryable.</param>
    /// <returns>A queryable of Modified history records.</returns>
    public static IQueryable<Models.AutoHistory> Modifications(
        this IQueryable<Models.AutoHistory> query)
    {
        return query.OfKind(EntityState.Modified);
    }

    /// <summary>
    /// Gets only Deleted records.
    /// </summary>
    /// <param name="query">The AutoHistory queryable.</param>
    /// <returns>A queryable of Deleted history records.</returns>
    public static IQueryable<Models.AutoHistory> Deletions(
        this IQueryable<Models.AutoHistory> query)
    {
        return query.OfKind(EntityState.Deleted);
    }

    /// <summary>
    /// Orders history records by most recent first.
    /// </summary>
    /// <param name="query">The AutoHistory queryable.</param>
    /// <returns>A queryable ordered by Created descending.</returns>
    public static IOrderedQueryable<Models.AutoHistory> MostRecentFirst(
        this IQueryable<Models.AutoHistory> query)
    {
        return query.OrderByDescending(h => h.Created);
    }

    /// <summary>
    /// Orders history records by oldest first.
    /// </summary>
    /// <param name="query">The AutoHistory queryable.</param>
    /// <returns>A queryable ordered by Created ascending.</returns>
    public static IOrderedQueryable<Models.AutoHistory> OldestFirst(
        this IQueryable<Models.AutoHistory> query)
    {
        return query.OrderBy(h => h.Created);
    }

    /// <summary>
    /// Parses the JSON changes into a strongly-typed object.
    /// </summary>
    /// <typeparam name="T">The entity type to deserialize to.</typeparam>
    /// <param name="history">The history record.</param>
    /// <param name="options">Optional JSON serializer options.</param>
    /// <returns>The parsed changes or null if parsing fails.</returns>
    public static Models.AutoHistoryChanges<T>? ParseChanges<T>(
        this Models.AutoHistory history,
        JsonSerializerOptions? options = null) where T : class
    {
        if (string.IsNullOrEmpty(history.Changed))
            return null;

        try
        {
            return JsonSerializer.Deserialize<Models.AutoHistoryChanges<T>>(
                history.Changed, options);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parses the JSON changes into a dictionary format.
    /// </summary>
    /// <param name="history">The history record.</param>
    /// <param name="options">Optional JSON serializer options.</param>
    /// <returns>The parsed changes as dictionaries or null if parsing fails.</returns>
    public static Models.AutoHistoryChanges<Dictionary<string, object?>>? ParseChangesAsDictionary(
        this Models.AutoHistory history,
        JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrEmpty(history.Changed))
            return null;

        try
        {
            return JsonSerializer.Deserialize<Models.AutoHistoryChanges<Dictionary<string, object?>>>(
                history.Changed, options);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the list of changed property names from a Modified history record.
    /// </summary>
    /// <param name="history">The history record.</param>
    /// <returns>List of property names that were changed, or empty list if parsing fails.</returns>
    public static IReadOnlyList<string> GetChangedPropertyNames(this Models.AutoHistory history)
    {
        var changes = history.ParseChangesAsDictionary();
        if (changes?.Before == null && changes?.After == null)
            return Array.Empty<string>();

        var propertyNames = new HashSet<string>();

        if (changes.Before != null)
            foreach (var key in changes.Before.Keys)
                propertyNames.Add(key);

        if (changes.After != null)
            foreach (var key in changes.After.Keys)
                propertyNames.Add(key);

        return propertyNames.ToList().AsReadOnly();
    }
}
