using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Outbox;

internal static class OutboxJson
{
    // Enums are written as names, so reordering an enum never changes
    // the meaning of events that are already in the table.
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
