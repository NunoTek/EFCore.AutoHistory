using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EFCore.AutoHistory.Helpers;

public static class SerializerHelper
{
    private static JsonSerializerOptions TextSerializerOptions(bool pascalCase = false) => new JsonSerializerOptions()
    {
        PropertyNamingPolicy = pascalCase ? null : JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
        AllowTrailingCommas = true,
        MaxDepth = 64,

        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    public static string Serialize(object value, bool pascalCase = false)
    {
        var options = TextSerializerOptions(pascalCase);

        return JsonSerializer.Serialize(value, options);
    }

    public static T? Deserialize<T>(string value)
    {
        if (string.IsNullOrEmpty(value))
            return (T)Activator.CreateInstance(typeof(T));

        // TODO: https://github.com/dotnet/runtime/issues/32291
        var options = TextSerializerOptions();

        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        return JsonSerializer.Deserialize<T>(value, options);
    }
}