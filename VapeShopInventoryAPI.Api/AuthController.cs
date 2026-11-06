using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VapeShopInventoryAPI.Api.DTOs;
using AppUser = VapeShopInventoryAPI.Api.User;

namespace VapeShopInventoryAPI.Api;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private const string InvalidCredentialsMessage = "Invalid username or password.";

    private readonly VapeShopInventoryDbContext _context;
    private readonly IPasswordHasher<AppUser> _hasher;
    private readonly TokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(VapeShopInventoryDbContext context, IPasswordHasher<AppUser> hasher, TokenService tokenService, ILogger<AuthController> logger)
    {
        _context = context;
        _hasher = hasher;
        _tokenService = tokenService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Unauthorized(new { message = InvalidCredentialsMessage });
        }

        var username = AppUser.NormalizeUsername(request.Username);
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            return Unauthorized(new { message = InvalidCredentialsMessage });
        }

        var verificationResult = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = InvalidCredentialsMessage });
        }

        var (token, expiresAt) = _tokenService.CreateToken(user);

        return Ok(new LoginResponse { Token = token, ExpiresAt = expiresAt });
    }
}