using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace StreamSchedule.GraphQL;

public class EnumConverter : StringEnumConverter
{
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        try { return base.ReadJson(reader, objectType, existingValue, serializer); }
        catch (JsonSerializationException)
        {
            Type enumType = Nullable.GetUnderlyingType(objectType) ?? objectType;

            if (enumType.IsEnum)
                return Enum.TryParse(enumType, "UNKNOWN", true, out object? unknownFallback) ? unknownFallback : Activator.CreateInstance(enumType);

            throw;
        }
    }
}
