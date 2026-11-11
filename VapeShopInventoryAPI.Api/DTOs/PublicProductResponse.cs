namespace VapeShopInventoryAPI.Api.DTOs;

public record PublicProductResponse
{
    public required int Id { get; init; }
    public required string Name { get; init; } = string.Empty;
    public required decimal Price { get; init; }
    public required bool IsInStock { get; init; }
    public required string Category { get; init; } = string.Empty;
}
