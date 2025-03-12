namespace EFCore.AutoHistory.Attributes;

/// <summary>
/// Represents the attribute to include the history.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class IncludeHistoryAttribute : Attribute
{
    public IncludeHistoryAttribute()
    {
    }
}