namespace VapeShopInventoryAPI.Api.DTOs;

public record CurrentUserResponse
{
    public required int Id { get; init; }
    public required string Username { get; init; }
    public required string Role { get; init; }
}
