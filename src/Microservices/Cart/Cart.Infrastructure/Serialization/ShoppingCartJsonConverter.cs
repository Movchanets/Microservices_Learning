using System.Text.Json;
using System.Text.Json.Serialization;
using Cart.Domain.Aggregates;

namespace Cart.Infrastructure.Serialization;

public sealed class ShoppingCartJsonConverter : JsonConverter<ShoppingCart>
{
    public override ShoppingCart? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // Support both PascalCase and camelCase
        var buyerId = GetStringProperty(root, "BuyerId", "buyerId");

        var cart = new ShoppingCart(buyerId);

        if (TryGetProperty(root, "Items", "items", out var itemsElement))
        {
            foreach (var item in itemsElement.EnumerateArray())
            {
                var sku = GetStringProperty(item, "Sku", "sku");
                var quantity = GetInt32Property(item, "Quantity", "quantity");
                cart.AddItem(sku, quantity);
            }
        }

        return cart;
    }

    public override void Write(Utf8JsonWriter writer, ShoppingCart value, JsonSerializerOptions options)
    {
        var namingPolicy = options.PropertyNamingPolicy;

        writer.WriteStartObject();

        writer.WriteString(ConvertName(namingPolicy, "BuyerId"), value.BuyerId);

        writer.WritePropertyName(ConvertName(namingPolicy, "Items"));
        writer.WriteStartArray();
        foreach (var item in value.Items)
        {
            writer.WriteStartObject();
            writer.WriteString(ConvertName(namingPolicy, "Sku"), item.Sku);
            writer.WriteNumber(ConvertName(namingPolicy, "Quantity"), item.Quantity);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
    }

    private static string ConvertName(JsonNamingPolicy? policy, string name) =>
        policy?.ConvertName(name) ?? name;

    private static string GetStringProperty(JsonElement element, string name1, string name2)
    {
        if (element.TryGetProperty(name1, out var prop))
            return prop.GetString()!;
        if (element.TryGetProperty(name2, out prop))
            return prop.GetString()!;
        return string.Empty;
    }

    private static int GetInt32Property(JsonElement element, string name1, string name2)
    {
        if (element.TryGetProperty(name1, out var prop))
            return prop.GetInt32();
        if (element.TryGetProperty(name2, out prop))
            return prop.GetInt32();
        return 0;
    }

    private static bool TryGetProperty(JsonElement element, string name1, string name2, out JsonElement value)
    {
        if (element.TryGetProperty(name1, out value))
            return true;
        if (element.TryGetProperty(name2, out value))
            return true;
        value = default;
        return false;
    }
}
