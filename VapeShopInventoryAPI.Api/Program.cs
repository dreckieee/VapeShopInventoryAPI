using Microsoft.EntityFrameworkCore;
using VapeShopInventoryAPI.Api;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(allowIntegerValues: true)));
builder.Services.AddDbContext<VapeShopInventoryDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("VapeShopInventory")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();

public partial class Program { }