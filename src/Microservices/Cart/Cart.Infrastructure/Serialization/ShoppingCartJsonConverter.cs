using System.Text.Json;
using System.Text.Json.Serialization;
using Cart.Domain.Aggregates;

namespace Cart.Infrastructure.Serialization;

public sealed class ShoppingCartJsonConverter : JsonConverter<ShoppingCart>
{
    public override ShoppingCart? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var buyerId = GetNullableGuidProperty(root, "BuyerId", "buyerId");
        var id = GetNullableGuidProperty(root, "Id", "id");

        var cart = new ShoppingCart(buyerId);
        if (id.HasValue)
        {
            typeof(ShoppingCart).GetProperty("Id")!.SetValue(cart, id.Value);
        }

        if (TryGetProperty(root, "Items", "items", out var itemsElement))
        {
            foreach (var item in itemsElement.EnumerateArray())
            {
                var productId = GetGuidProperty(item, "ProductId", "productId");
                var quantity = GetInt32Property(item, "Quantity", "quantity");
                var price = GetDecimalProperty(item, "Price", "price");
                var storeId = GetGuidProperty(item, "StoreId", "storeId");
                cart.AddItem(productId, quantity, storeId, price);
            }
        }

        return cart;
    }

    public override void Write(Utf8JsonWriter writer, ShoppingCart value, JsonSerializerOptions options)
    {
        var p = options.PropertyNamingPolicy;
        writer.WriteStartObject();
        writer.WriteString(ConvertName(p, "Id"), value.Id);
        if (value.BuyerId.HasValue)
            writer.WriteString(ConvertName(p, "BuyerId"), value.BuyerId.Value);
        else
            writer.WriteNull(ConvertName(p, "BuyerId"));
        writer.WriteNumber(ConvertName(p, "Version"), value.Version);
        writer.WriteString(ConvertName(p, "CreatedAt"), value.CreatedAt);
        writer.WriteString(ConvertName(p, "UpdatedAt"), value.UpdatedAt);

        writer.WritePropertyName(ConvertName(p, "Items"));
        writer.WriteStartArray();
        foreach (var item in value.Items)
        {
            writer.WriteStartObject();
            writer.WriteString(ConvertName(p, "ProductId"), item.ProductId);
            writer.WriteString(ConvertName(p, "StoreId"), item.StoreId);
            writer.WriteNumber(ConvertName(p, "Quantity"), item.Quantity);
            writer.WriteNumber(ConvertName(p, "Price"), item.Price);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static string ConvertName(JsonNamingPolicy? policy, string name) => policy?.ConvertName(name) ?? name;
    private static int GetInt32Property(JsonElement el, string n1, string n2) =>
        el.TryGetProperty(n1, out var p) ? p.GetInt32() : el.TryGetProperty(n2, out p) ? p.GetInt32() : 0;
    private static decimal GetDecimalProperty(JsonElement el, string n1, string n2) =>
        el.TryGetProperty(n1, out var p) ? p.GetDecimal() : el.TryGetProperty(n2, out p) ? p.GetDecimal() : 0m;
    private static Guid GetGuidProperty(JsonElement el, string n1, string n2) =>
        el.TryGetProperty(n1, out var p) && p.TryGetGuid(out var g) ? g :
        el.TryGetProperty(n2, out p) && p.TryGetGuid(out g) ? g : Guid.Empty;
    private static Guid? GetNullableGuidProperty(JsonElement el, string n1, string n2) =>
        el.TryGetProperty(n1, out var p) && p.ValueKind == JsonValueKind.Null ? null :
        el.TryGetProperty(n1, out p) && p.TryGetGuid(out var g) ? g :
        el.TryGetProperty(n2, out p) && p.ValueKind == JsonValueKind.Null ? null :
        el.TryGetProperty(n2, out p) && p.TryGetGuid(out g) ? g : null;
    private static bool TryGetProperty(JsonElement el, string n1, string n2, out JsonElement v)
    {
        if (el.TryGetProperty(n1, out v)) return true;
        if (el.TryGetProperty(n2, out v)) return true;
        v = default; return false;
    }
}
