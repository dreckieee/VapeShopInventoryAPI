using Microsoft.AspNetCore.Mvc;

namespace VapeShopInventoryAPI.Tests;

[ApiController]
[Route("test-only/unmarked")]
public class UnmarkedTestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok();
}
