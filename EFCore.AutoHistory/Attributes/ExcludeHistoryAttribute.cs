namespace EFCore.AutoHistory.Attributes;

/// <summary>
/// Represents the attribute to exclude the history.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ExcludeHistoryAttribute : Attribute
{
    public ExcludeHistoryAttribute()
    {
    }
}