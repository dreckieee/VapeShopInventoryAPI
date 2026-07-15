using System.Text.RegularExpressions;
using Microsoft.Playwright.NUnit;

namespace VapeShopInventoryAPI.Tests;

public class Tests : PageTest
{
    [Test]
    public async Task Test1()
    {
        await Page.GotoAsync(url: "https://playwright.dev");
        await Expect(Page).ToHaveTitleAsync(new Regex("Playwright"));
    }
}
