using Microsoft.EntityFrameworkCore;
using VapeShopInventoryAPI.Api;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(allowIntegerValues: true)));
builder.Services.AddDbContext<VapeShopInventoryDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("VapeShopInventory")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton(serviceProvider =>
    JwtSettings.FromConfiguration(
        serviceProvider.GetRequiredService<IConfiguration>()));
builder.Services.AddSingleton<TokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme).Configure<JwtSettings>((options, settings) =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = settings.CreateSigningKey(),
        ValidateIssuer = true,
        ValidIssuer = settings.Issuer,
        ValidateAudience = true,
        ValidAudience = settings.Audience,
        ValidateLifetime = true,
        ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
        NameClaimType = "name",
        RoleClaimType = "role",
    };
});

var app = builder.Build();

app.Services.GetRequiredService<JwtSettings>(); // resolve once so a bad Jwt setting stops startup

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<VapeShopInventoryDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
    await UserSeeder.SeedAsync(context, app.Configuration, hasher);
}

app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();

public partial class Program { }