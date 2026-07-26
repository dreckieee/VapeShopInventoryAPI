# Vape Shop Inventory API

ASP.NET Core Web API for inventory management — built for a real Vape Shop business.

## Status: In Progress
Build 1 (Product CRUD) and Build 2 (Expense CRUD) complete and tested end-to-end, including unique SKU constraint, structured exception handling, and DTO-based update binding. Build 3 (Sale + SaleItem) is fully complete end-to-end: domain layer, EF Core migrations, DTOs, `SalesController` (Create, Get, EditSaleDate, CloseSale, CancelSale), and `SaleItemsController` (AddSaleItem, ReduceSaleItemQuantity) are all implemented and tested. All response DTOs (`ProductResponse`, `SaleResponse`, `SaleItemResponse`) use a `record` + `init`-only-property + static factory pattern (Day 91). The test suite has grown to 11 tests (4 Products, 7 Sales/SaleItems), covering the full Sale lifecycle including quantity reduction (partial and to-zero) and sale closure, and running fully in-process via `WebApplicationFactory` against an isolated in-memory database. Two real data-integrity bugs were found and fixed via this test coverage (Day 92 — see log below). Deployment remains the only item blocking Step 2 completion — see Day 89 log for current hosting-provider status.

## Tech Stack
- .NET 10 / ASP.NET Core (Controllers)
- Entity Framework Core
- SQLite

## Testing
- `VapeShopInventoryAPI.Tests` — dedicated NUnit test project (Day 83)
- **Integration testing approach (revised Day 91):** tests use `WebApplicationFactory<Program>` via a custom `CustomWebApplicationFactory`, which boots the API in-process and swaps the real SQLite database for an isolated in-memory SQLite connection (kept open for the test run's lifetime, with schema created via `EnsureCreated()`). Requests are made with `HttpClient` (`_factory.CreateClient()`), not a real network call.
- **Products (4 tests):** `GetProducts_ReturnsSuccessAndNonEmptyList`, `GetProduct_NonExistentId_ReturnsNotFound`, `CreateProduct_ValidProduct_ReturnsCreated`, `CreateProduct_InvalidProduct_ReturnsBadRequest`.
- **Sales/SaleItems (7 tests):** `GetSale_NonExistentId_ReturnsNotFound`, `CreateSale_ValidSaleRequest_ReturnsCreated`, `GetSale_ExistingId_ReturnsOk`, `AddSaleItem_ValidRequest_ReturnsOk`, `ReduceSaleItemQuantity_ValidRequest_ReturnsOk` (partial reduction), `ReduceSaleItemQuantity_ReducesToZero_ReturnsOk` (full reduction — the regression test for the Day 92 orphaned-entity bug), `CloseSale_ValidSale_ReturnsOk`.
- **Cleanup:** Sales tests clean up via `PUT /api/Sales/{id}/cancel`, which handles both empty sales and sales with items uniformly (no plain delete endpoint exists for sales by design — see Design decision below). Closed sales cannot be cancelled by design; teardown detects this via a per-test flag and logs an explicit skip message rather than attempting and failing cleanup. Product cleanup is skipped the same way when a closed sale's line item still references it.
- All 11 tests pass via `dotnet test`, with no manual server-start step and no dependency on real dev data.
- Purpose: groundwork for Step 3 of the roadmap (Playwright + NUnit portfolio item). Playwright itself remains in the project, reserved for genuine browser/UI automation once the Blazor UI phase begins.

## Roadmap Checklist

- [x] Build 1 — Product CRUD (GET/POST/PUT/DELETE)
- [x] Build 2 — Expense CRUD (GET/POST/PUT/DELETE)
- [x] Build 3 — Sale + SaleItem (1-to-many)
- [ ] Deployment (in progress — see Day 89 log)

## Tech notes
- SQLite maps `decimal` → `TEXT` (exact precision, avoids float rounding vs REAL)
- No magic strings: table/column names resolved dynamically via `_context.Model`
- Response DTOs never expose raw domain entities — avoids circular reference and keeps internal fields out of the API contract
- API responses serialize with camelCase JSON property names; `System.Net.Http.Json`'s `ReadFromJsonAsync<T>()` handles this by default
- Negative ids (e.g. `-1`) are a reliable choice for "guaranteed non-existent" test data
- **Response DTO construction (Day 91):** response DTOs mapped from a domain entity are `record` types with `init`-only properties, plus a static factory method for mapping logic.
- `SaleResponse.FromSale` maps its nested `SaleItems` collection via `.Select(SaleItemResponse.FromSaleItem).ToList()`, reusing `SaleItemResponse`'s own factory.
- `Program.cs` declares `public partial class Program { }` at file scope so the test project can reference the app's entry point.

### Design decision: stock deduction timing
Stock is deducted from `Product.StockQuantity` at `CloseSale` time, not at `AddSaleItem` time. Built for a single-register, in-person retail context — not designed for multiple simultaneous registers against the same stock pool (accepted, documented tradeoff).

### Design decision: `CancelSale` scope and limitation
`CancelSale` permanently deletes an open sale and any attached `SaleItem` rows, uniformly regardless of item count, since nothing affects stock until `CloseSale` runs. No audit trail is kept for cancelled sales — acceptable since an unclosed sale was never a completed transaction. Once a sale is closed, it cannot be deleted or cancelled through the API by design.

### Design decision: FK relationships use `Restrict`, not `Cascade`
Both `SaleItem → Sale` and `SaleItem → Product` foreign keys use `OnDelete(DeleteBehavior.Restrict)` rather than `Cascade`, deliberately. `Cascade` would silently delete child rows whenever a parent is removed — convenient, but it hides deletion behavior inside EF Core's internals rather than making it visible in application code. For a POS system where sale/inventory audit trails matter, an explicit, visible removal path (see below) is safer than an implicit one, even at the cost of extra code.

### Known issue (fixed Day 92): orphaned `SaleItem` rows on quantity-reduce-to-zero
`Sale.ReduceSaleItemQuantity` removes a `SaleItem` from its in-memory collection once its quantity reaches zero. Because the `SaleItem → Sale` foreign key is `Restrict` and required (non-nullable), EF Core has no valid action for an entity severed from its parent's tracked collection without an explicit instruction — it threw `DbUpdateException` on `SaveChangesAsync()`. Fixed in `SaleItemsController.ReduceSaleItemQuantity` by checking whether the item was removed from `sale.SaleItems` after the domain call, and explicitly calling `_context.SaleItems.Remove(saleItem)` before saving. Covered by `ReduceSaleItemQuantity_ReducesToZero_ReturnsOk`.

### Known issue (fixed Day 92): `DeleteProduct` had no guard against existing sale records
`ProductsController.DeleteProduct` previously attempted deletion unconditionally, resulting in a bare `500 Internal Server Error` (an unhandled `DbUpdateException`) whenever a product had any existing `SaleItem` reference. Fixed by adding an explicit `_context.SaleItems.AnyAsync(...)` check before deletion, returning `409 Conflict` with a clear message when references exist, plus a fallback `catch (DbUpdateException)` for any other unexpected constraint failure.

## Endpoints

### Products
- `GET /api/Products` — list all products
- `GET /api/Products/{id}` — get product by id
- `POST /api/Products` — create product (returns `400 BadRequest` on invalid input)
- `PUT /api/Products/{id}` — update product
- `DELETE /api/Products/{id}` — delete product (returns `409 Conflict` if the product has existing sale item references)

### Expenses
- `GET /api/Expenses` — list all expenses
- `GET /api/Expenses/{id}` — get expense by id
- `POST /api/Expenses` — create expense
- `PUT /api/Expenses/{id}` — update expense
- `DELETE /api/Expenses/{id}` — delete expense

### Sales
- `POST /api/Sales` — create a new sale
- `GET /api/Sales/{id}` — get a sale with its items
- `PATCH /api/Sales/{id}/date` — edit the sale date
- `POST /api/Sales/{id}/close` — finalize a sale, decrementing stock
- `PUT /api/Sales/{id}/cancel` — permanently cancel an open sale

### Sale Items
- `POST /api/Sales/{saleId}/items` — add an item to a sale
- `PATCH /api/Sales/{saleId}/items/{itemId}/reduce` — reduce an item's quantity

## Day 93 — Test suite design cleanup; Azure cancellation confirmed; deployment still pending

**Context:** Day 93 was slated for DigitalOcean deployment (carried over from Day 92). No deployment work happened this session due to time constraints. Instead, existing test coverage from Day 92 was reviewed and a real test-design bug was found and fixed in `AddSaleItem_ValidRequest_ReturnsOk`.

**Bug found — self-referential assertion, no actual coverage:** `AddSaleItem_ValidRequest_ReturnsOk` looked up a sale item via `sale.SaleItems.Find(...)` and asserted its fields against `saleItem` — but `saleItem` came from the exact same `Find()` call on the exact same `sale` object one level up. The assertions were comparing a value to itself, so they could not fail regardless of whether `AddSaleItem`'s response actually mapped fields correctly. Root cause traced back to `CreateSaleWithItemAsync` (the shared arrange-helper) also asserting the full `AddSaleItem` contract internally — duplicating what the test itself should own.

**Fix:** `CreateSaleWithItemAsync` reverted to minimal arrange-only assertions (status OK, not-null), matching the existing pattern already used by `CreateTestSaleAsync` and `CreateTestProductAsync`. `AddSaleItem_ValidRequest_ReturnsOk` now asserts its own `ProductId`/`Quantity`/`UnitPriceAtSale` directly against known test inputs (`product.Id`, `saleItemQuantity`, `product.Price`), not against a second lookup of the same object. Net effect: this closes a real, previously-silent coverage gap on `AddSaleItem`'s response contract — it was not actually being tested before this session despite existing test code.

**Standing principle reinforced:** shared arrange-helpers used across multiple tests should assert only enough to fail fast on broken setup — not the specific contract each individual test exists to verify. Assertion ownership belongs to the test that names the behavior.

**Git hygiene:** one commit this session — `test:` SalesApiTests.cs (fixed self-referential assertion in AddSaleItem test, reverted helper to arrange-only).

**Azure cleanup confirmed:** Azure Pay-As-You-Go subscription cancellation (initiated Day 89) verified complete — Subscriptions page shows 0 active subscriptions.

**Deployment status:** still deferred — DigitalOcean account creation, Droplet provisioning (2GB RAM / 1 vCPU, Ubuntu 24.04 LTS, Singapore region — spec finalized Day 90), and full deployment (SSH, `dotnet publish`, systemd, firewall) now carry over to Day 94.

## Day 92 — Two production bugs found and fixed via test coverage; deployment deferred again

**Test coverage added:** `AddSaleItem_ValidRequest_ReturnsOk`, `ReduceSaleItemQuantity_ValidRequest_ReturnsOk` (partial reduction, also asserts `ReductionFrequency`/`TotalQuantityReduction` counters), `ReduceSaleItemQuantity_ReducesToZero_ReturnsOk`, `CloseSale_ValidSale_ReturnsOk` (asserts stock deduction on close). Test suite grew from 7 to 11 tests.

**Bug #1 — orphaned `SaleItem` on quantity-reduce-to-zero:** writing the reduce-to-zero test surfaced a real `DbUpdateException` — `Sale.ReduceSaleItemQuantity` removes the item from its in-memory list at zero quantity, but the `SaleItem → Sale` FK (`Restrict`, required) meant EF Core had no valid action for the resulting orphan. Root-caused via reading the actual exception body (initially masked by only checking HTTP status codes in test cleanup) rather than assuming. Fixed in the controller by explicitly removing the orphaned entity from `_context` before saving. See Known issues section above for full detail.

**Bug #2 — `DeleteProduct` had no FK guard:** while testing `CloseSale`'s teardown (a closed sale's product can never be deleted, by the same `Restrict` FK), discovered `DeleteProduct` had no explicit check at all — any product with sale history returned a bare `500` instead of a clean error. Fixed with a pre-emptive `AnyAsync` existence check returning `409 Conflict`, plus a narrow fallback catch for genuinely unexpected `DbUpdateException` cases.

**Teardown refinement:** simplified sale/product cleanup to rely on `CancelSale` alone (which already handles child-row deletion correctly via same-transaction removal, sidestepping the FK issue entirely) rather than a manual reduce-then-cancel sequence. Added explicit skip-logging for closed-sale cleanup instead of letting expected, by-design failures surface as generic warnings.

**Git/workflow:** formalized `test:` as a new commit category (previously only `feat:/fix:/docs:/refactor:` existed), since new test coverage doesn't cleanly fit `feat:`.

**Deployment status:** still deferred — Day 92 was originally slated for DigitalOcean deployment, but test-writing surfaced two real production bugs worth fixing first. DigitalOcean account creation, Droplet provisioning, and full deployment now carry over to the next session.

## Day 91 — DTO pattern revision; test infrastructure overhaul (WebApplicationFactory + HttpClient); deployment still on hold

**DTO pattern correction:** the Day 90 private-constructor + `[JsonConstructor]` pattern was reconsidered as over-engineered for flat response DTOs with no real invariant to protect — the need for `[JsonConstructor]` was itself a signal the pattern fought `System.Text.Json`'s natural model. Revised all three response DTOs to `record` types with `init`-only properties and static factory methods (`FromProduct`, `FromSale`, `FromSaleItem`), replacing duplicated inline construction across `ProductsController`, `SalesController`, and `SaleItemsController`. Verified via `dotnet test`: no existing test needed modification, since `JsonSerializer.Deserialize<T>` never depended on a DTO's internal construction mechanism.

**Initial Sales test coverage:** added `SalesApiTests.cs` with 3 tests, following the existing Products test pattern, using a shared `CreateTestSaleAsync` helper.

**Self-identified gap — test isolation:** writing Sales tests surfaced two real problems with the existing test setup: (1) all tests ran against the real dev SQLite database rather than an isolated one, silently polluting real data on every run, and (2) `CancelSale` cannot remove a *closed* sale by design, so any future test exercising `CloseSale` would leave a permanent, untestable row behind. A public delete endpoint was considered and rejected (see Design decision above) as the wrong tool — it would solve a test problem by creating a production risk.

**Test infrastructure overhaul:** adopted `WebApplicationFactory<Program>` for in-process integration testing:
- Added `public partial class Program { }` to `Program.cs`, required for the test project to reference the app's entry point
- Built `CustomWebApplicationFactory`, which overrides `ConfigureWebHost` to remove the real `DbContextOptions<VapeShopInventoryDbContext>` registration and replace it with one backed by an in-memory SQLite connection, kept open for the factory's lifetime
- **Playwright reconsidered for API-level testing:** `IAPIRequestContext` requires a real HTTP server and cannot talk to an in-process `TestServer`. Both `ProductsApiTests` and `SalesApiTests` were migrated from `IAPIRequestContext` to plain `HttpClient`. Playwright/NUnit remains in the project, reserved for genuine browser automation once the Blazor UI phase begins
- **Self-identified gap closed during migration:** `GetProducts_ReturnsSuccessAndNonEmptyList` previously passed only because it silently read real, populated dev data. Fixed by having the test seed its own product first
- All 7 tests (4 Products, 3 Sales) verified passing via `dotnet test`, fully self-contained

**Deployment status:** still deferred. DigitalOcean account creation and Droplet provisioning to follow next session.

## Day 90 — ProductResponse construction locked down (superseded Day 91 — see above); deployment on hold (card unavailable)

**DTO refactor:** `ProductResponse` previously used public setters, allowing it to be constructed with arbitrary values from anywhere in the codebase. Refactored to `private set` properties, a private constructor, and a static `FromProduct(Product product)` factory method as the only construction path. Wired into all three read/write endpoints in `ProductsController`.

**Test suite fix:** the above change broke deserialization in `ProductsApiTests`, which had been deserializing into `Product` rather than `ProductResponse`. Fixed by updating affected tests and adding `[JsonConstructor]` to the private constructor.

**Housekeeping:** removed `Tests.cs`, a leftover default Playwright scaffold test causing a false failure unrelated to this project's actual test suite.

**Deployment status:** DigitalOcean account creation deferred — card temporarily unavailable. Planned Droplet spec: 2GB RAM / 1 vCPU Basic Droplet (~$12/month), Ubuntu 24.04 LTS, Singapore region.

## Day 89 — Deployment troubleshooting: two providers blocked on capacity

**Oracle Cloud (originally selected, account went live Day 88):** Attempted VM creation using `VM.Standard.A1.Flex` in Singapore West (AD-1). Both attempts failed with `Out of capacity for shape VM.Standard.A1.Flex in availability domain AD-1` — a known, widely-reported Always Free tier issue, not an account or configuration problem. `VM.Standard.E2.1.Micro` not offered on this tenancy, confirming newer Oracle accounts only receive Ampere-based Always Free resources.

**Azure (fallback attempt):** Created a new Pay-As-You-Go Azure account same-day (selected over Free Trial specifically because Free Trial subscriptions cannot request quota increases). Attempting to select `Standard_B1s` in Southeast Asia surfaced a `Request quota` requirement; Azure's own quota-recommendation tool confirmed **B-series is unavailable in Southeast Asia entirely** — a regional capacity constraint. Cancelled the Azure subscription same-day.

**Decision:** pivot to DigitalOcean (paid, no free-tier capacity lottery) — chosen over Hetzner due to Hetzner's biometric ID verification requirement, which poses real rejection risk given available ID documents on hand.

**No structural regret on Oracle/Azure attempts** — both were reasonable, well-reasoned choices at the time; the blockers were genuine provider-side capacity constraints affecting many users, not planning errors.

## About
Part of my transition into remote software engineering (QA Automation → SDET → Full-Stack).
Daily build-in-public log and full C# learning history: [github.com/dreckieee/csharp](https://github.com/dreckieee/csharp)

## How to Run Locally
run `dotnet run`
Then open `http://localhost:{port}/swagger` in your browser to explore the API.

Note: swap {port} to your local port — check your terminal output for the exact URL after running `dotnet run`.

To run tests: `dotnet test` from `VapeShopInventoryAPI.Tests` — no separate server needs to be running first; the test suite boots the API in-process against an isolated in-memory database.