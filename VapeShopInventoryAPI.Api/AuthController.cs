using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using VapeShopInventoryAPI.Api.DTOs;
using AppUser = VapeShopInventoryAPI.Api.User;

namespace VapeShopInventoryAPI.Api;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private const string InvalidCredentialsMessage = "Invalid username or password.";
    private const string InvalidTokenClaimsMessage = "Token is missing required claims.";

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

    [Authorize]
    [HttpGet("me")]
    public ActionResult<CurrentUserResponse> GetCurrentUser()
    {
        var idClaim = User.FindFirst("sub")?.Value;
        var usernameClaim = User.FindFirst("name")?.Value;
        var roleClaim = User.FindFirst("role")?.Value;

        if (!int.TryParse(idClaim, out var id) || usernameClaim == null || roleClaim == null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = InvalidTokenClaimsMessage });
        }
        return Ok(new CurrentUserResponse
        {
            Id = id,
            Username = usernameClaim,
            Role = roleClaim
        });
    }
}
