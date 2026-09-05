namespace VapeShopInventoryAPI.Tests;
using System.Text.Json.Serialization;
using System.Text.Json;

public static class TestJsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: true) }
    };
}