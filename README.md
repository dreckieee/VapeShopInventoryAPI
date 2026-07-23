# Vape Shop Inventory API

ASP.NET Core Web API for inventory management — built for a real Vape Shop business.

## Status: In Progress
Build 1 (Product CRUD) and Build 2 (Expense CRUD) complete and tested end-to-end, including unique SKU constraint, structured exception handling, and DTO-based update binding. Build 3 (Sale + SaleItem) is fully complete end-to-end: domain layer, EF Core migrations, DTOs, `SalesController` (Create, Get, EditSaleDate, CloseSale, CancelSale), and `SaleItemsController` (AddSaleItem, ReduceSaleItemQuantity) are all implemented and tested. The self-identified cancel-empty-sale gap (Day 79) is now closed. All response DTOs (`ProductResponse`, `SaleResponse`, `SaleItemResponse`) use a `record` + `init`-only-property + static factory pattern (Day 91), superseding the private-constructor approach from Day 90. The test suite (7 tests: 4 Products, 3 Sales) has been fully migrated from Playwright's API-testing feature to `WebApplicationFactory` + `HttpClient` (Day 91) — tests now run fully in-process against an isolated in-memory database, with no manual server-start step required. Deployment remains the only item blocking Step 2 completion — see Day 89 log for current hosting-provider status.

## Tech Stack
- .NET 10 / ASP.NET Core (Controllers)
- Entity Framework Core
- SQLite

## Testing
- `VapeShopInventoryAPI.Tests` — dedicated NUnit test project (Day 83)
- **Integration testing approach (revised Day 91):** tests use `WebApplicationFactory<Program>` via a custom `CustomWebApplicationFactory`, which boots the API in-process and swaps the real SQLite database for an isolated in-memory SQLite connection (kept open for the test run's lifetime, with schema created via `EnsureCreated()`). Requests are made with `HttpClient` (`_factory.CreateClient()`), not a real network call. This replaced an earlier Playwright-based approach — see "Day 91 — Test infrastructure overhaul" below for the full reasoning
- **Products (4 tests):** `GetProducts_ReturnsSuccessAndNonEmptyList`, `GetProduct_NonExistentId_ReturnsNotFound`, `CreateProduct_ValidProduct_ReturnsCreated`, `CreateProduct_InvalidProduct_ReturnsBadRequest`. `GetProducts_ReturnsSuccessAndNonEmptyList` seeds its own product via a shared `CreateTestProductAsync` helper rather than assuming any product already exists — keeps the test independent of database state and execution order
- **Sales (3 tests):** `GetSale_NonExistentId_ReturnsNotFound`, `CreateSale_ValidSaleRequest_ReturnsCreated`, `GetSale_ExistingId_ReturnsOk`. `GetSale_ExistingId_ReturnsOk` creates its own sale via a shared `CreateTestSaleAsync` helper (returning a named tuple of the response and deserialized DTO) rather than assuming any sale ID already exists
- Cleanup: Products tests delete via `DELETE /api/Products/{id}`; Sales tests clean up via `PUT /api/Sales/{id}/cancel`, since no plain delete endpoint exists for sales by design (see Design decision below)
- `[OneTimeSetUp]`/`[OneTimeTearDown]` create and dispose the shared `CustomWebApplicationFactory`/`HttpClient` once per test class; `[SetUp]`/`[TearDown]` (where used) handle per-test cleanup
- All 7 tests pass via `dotnet test`, with no manual server-start step and no dependency on real dev data
- Purpose: groundwork for Step 3 of the roadmap (Playwright + NUnit portfolio item). Playwright itself remains in the project and reserved for genuine browser/UI automation once the Blazor UI phase begins — see Day 91 log for why it was removed from API-level tests specifically

## Roadmap Checklist

- [x] Build 1 — Product CRUD (GET/POST/PUT/DELETE)
- [x] Build 2 — Expense CRUD (GET/POST/PUT/DELETE)
- [x] Build 3 — Sale + SaleItem (1-to-many)
- [ ] Deployment (in progress — see Day 89 log)

## Tech notes
- SQLite maps `decimal` → `TEXT` (exact precision, avoids float rounding vs REAL)
- No magic strings: table/column names resolved dynamically via `_context.Model`
- Response DTOs never expose raw domain entities — avoids circular reference and keeps internal fields out of the API contract
- API responses serialize with camelCase JSON property names; `System.Net.Http.Json`'s `ReadFromJsonAsync<T>()` handles this by default (uses `JsonSerializerDefaults.Web` internally), unlike manual `JsonSerializer.Deserialize` calls which needed an explicit `PropertyNameCaseInsensitive = true` option
- Negative ids (e.g. `-1`) are a reliable choice for "guaranteed non-existent" test data — auto-incrementing primary keys never produce negative values regardless of database state
- **Response DTO construction (revised Day 91):** response DTOs mapped from a domain entity (`ProductResponse`, `SaleResponse`, `SaleItemResponse`) are `record` types with `init`-only properties, plus a static factory method (`FromProduct`, `FromSale`, `FromSaleItem`) for mapping logic. This replaces the Day 90 private-constructor + `[JsonConstructor]` approach, which required extra plumbing to work with `System.Text.Json` and offered no real benefit for a DTO with no invariants to protect. `init` properties deserialize natively with no custom constructor needed; the factory method remains as a convenience for mapping, not the sole construction path. Using `record` also gives free value-equality, useful for test assertions comparing expected vs. actual deserialized responses. Input DTOs (`CreateProductRequest`, `UpdateProductRequest`, `CreateSaleRequest`, etc.) are unaffected — they keep public setters, since framework model-binding needs to construct them externally and they have no invariant to protect
- `SaleResponse.FromSale` maps its nested `SaleItems` collection via `.Select(SaleItemResponse.FromSaleItem).ToList()`, reusing `SaleItemResponse`'s own factory rather than duplicating item-mapping logic
- `Program.cs` declares `public partial class Program { }` at file scope so the test project can reference the app's entry point — required for `WebApplicationFactory<Program>` to work, since top-level statement programs generate an `internal` `Program` class by default

### Design decision: stock deduction timing
Stock is deducted from `Product.StockQuantity` at `CloseSale` time, not at `AddSaleItem` time. Built for a single-register, in-person retail context — not designed for multiple simultaneous registers against the same stock pool (accepted, documented tradeoff).

### Design decision: `CancelSale` scope and limitation
`CancelSale` permanently deletes an open sale and any attached `SaleItem` rows, uniformly regardless of item count, since nothing affects stock until `CloseSale` runs. No audit trail is kept for cancelled sales — acceptable since an unclosed sale was never a completed transaction. Once a sale is closed, it cannot be deleted or cancelled through the API by design — a closed sale represents a completed transaction, not something any API consumer should be able to remove. (A public delete endpoint was considered purely to simplify test cleanup for closed-sale test cases, but rejected: it would give every API consumer permanent delete access to completed transactions, reintroducing an audit-trail risk to solve a test-only problem. Test isolation is solved instead via `WebApplicationFactory`'s in-memory database, not by expanding the production API surface.)

## Endpoints

### Products
- `GET /api/Products` — list all products
- `GET /api/Products/{id}` — get product by id
- `POST /api/Products` — create product (returns `400 BadRequest` on invalid input)
- `PUT /api/Products/{id}` — update product
- `DELETE /api/Products/{id}` — delete product

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

## Day 91 — DTO pattern revision; test infrastructure overhaul (WebApplicationFactory + HttpClient); deployment still on hold

**DTO pattern correction:** the Day 90 private-constructor + `[JsonConstructor]` pattern was reconsidered as over-engineered for flat response DTOs with no real invariant to protect — the need for `[JsonConstructor]` was itself a signal the pattern fought `System.Text.Json`'s natural model. Revised all three response DTOs to `record` types with `init`-only properties and static factory methods (`FromProduct`, `FromSale`, `FromSaleItem`), replacing duplicated inline construction across `ProductsController`, `SalesController`, and `SaleItemsController`. Verified via `dotnet test`: no existing test needed modification, since `JsonSerializer.Deserialize<T>` never depended on a DTO's internal construction mechanism.

**Initial Sales test coverage:** added `SalesApiTests.cs` with 3 tests, following the existing Products test pattern, using a shared `CreateTestSaleAsync` helper.

**Self-identified gap — test isolation:** writing Sales tests surfaced two real problems with the existing test setup: (1) all tests ran against the real dev SQLite database rather than an isolated one, silently polluting real data on every run, and (2) `CancelSale` cannot remove a *closed* sale by design, so any future test exercising `CloseSale` would leave a permanent, untestable row behind. A public delete endpoint was considered and rejected (see Design decision above) as the wrong tool — it would solve a test problem by creating a production risk.

**Test infrastructure overhaul:** adopted `WebApplicationFactory<Program>` for in-process integration testing:
- Added `public partial class Program { }` to `Program.cs`, required for the test project to reference the app's entry point
- Built `CustomWebApplicationFactory`, which overrides `ConfigureWebHost` to remove the real `DbContextOptions<VapeShopInventoryDbContext>` registration and replace it with one backed by an in-memory SQLite connection, kept open for the factory's lifetime (in-memory SQLite discards its data the instant its connection closes, so EF Core's normal per-operation connection handling would otherwise reset the database on every call). Schema is created via `EnsureCreated()` against a scoped `DbContext` built from the modified service collection
- **Playwright reconsidered for API-level testing:** `IAPIRequestContext` (Playwright's API-testing feature) requires a real HTTP server and cannot talk to an in-process `TestServer` — this was the root cause of needing a manually-started server in a separate terminal all along. Playwright's actual industry role is browser/E2E automation, not backend API testing; using it purely for HTTP assertions was non-idiomatic for this specific job and structurally incompatible with in-process hosting. Both `ProductsApiTests` and `SalesApiTests` were migrated from `IAPIRequestContext` to plain `HttpClient` (via `_factory.CreateClient()`), the standard ASP.NET Core integration-testing pattern. All existing test coverage was preserved — same assertions, same intent, different transport mechanism. Playwright/NUnit remains in the project, reserved for genuine browser automation once the Blazor UI phase begins
- **Self-identified gap closed during migration:** `GetProducts_ReturnsSuccessAndNonEmptyList` previously passed only because it silently read real, populated dev data. Once tests ran against a genuinely empty isolated database, this test failed — correctly exposing a hidden dependency on external state. Fixed by having the test seed its own product via `CreateTestProductAsync` before asserting a non-empty list, matching the same "don't assume state exists" principle already applied to the Sales tests
- All 7 tests (4 Products, 3 Sales) verified passing via `dotnet test`, fully self-contained — no manual server start required, a direct prerequisite for eventually running this suite in GitHub Actions CI/CD

**Deployment status:** still deferred. DigitalOcean account creation and Droplet provisioning to follow next session.

## Day 90 — ProductResponse construction locked down (superseded Day 91 — see above); deployment on hold (card unavailable)

**DTO refactor:** `ProductResponse` previously used public setters, allowing it to be constructed with arbitrary values from anywhere in the codebase — inconsistent with its role as a controlled mapping of a real `Product`. Refactored to:
- All properties changed to `private set`
- Added a `private` constructor taking all mapped values
- Added a static `FromProduct(Product product)` factory method as the only way to construct a `ProductResponse`
- Removed the now-redundant `required` modifiers (previously needed to catch missing fields at external call sites — no longer relevant once external construction is impossible)
- Wired `FromProduct` into all three read/write endpoints in `ProductsController` (`GetProduct`, `GetProducts`, `CreateProduct`), replacing duplicated inline object-initializer blocks
- Removed `[JsonInclude]` from `Product.Id`, since raw `Product` entities are no longer serialized directly in any response

**Test suite fix:** the above change broke deserialization in `ProductsApiTests`, which had been deserializing API responses into `Product` (the entity) rather than `ProductResponse`. Since `Product.Id` no longer has `[JsonInclude]`, deserializing into `Product` silently left `Id` at `0`. Fixed by:
- Updating both affected tests to deserialize into `ProductResponse` instead of `Product`
- Adding `[JsonConstructor]` to `ProductResponse`'s private constructor, so `System.Text.Json` can construct valid instances directly from JSON (matching constructor parameter names to JSON fields) without needing public setters
- Verified via `dotnet test`: all 4 `ProductsApiTests` pass, confirming no regression

**Housekeeping:** removed `Tests.cs`, a leftover default Playwright scaffold test (`Test1`, navigating to playwright.dev) that was causing a false failure on every test run unrelated to this project's actual test suite.

**Deployment status:** DigitalOcean account creation deferred — card temporarily unavailable (held by a family member). Planned Droplet spec decided ahead of time: 2GB RAM / 1 vCPU Basic Droplet (~$12/month), Ubuntu 24.04 LTS, Singapore region — chosen over the cheaper 1GB tier for headroom (on-box `dotnet publish`, future Blazor UI phase) given cost is not the deciding factor for this project. Deployment steps (account creation, Droplet creation, SSH/systemd/firewall setup) resume next session.

## Day 89 — Deployment troubleshooting: two providers blocked on capacity

**Oracle Cloud (originally selected, account went live Day 88):** Attempted VM creation using the planned Always-Free-eligible `VM.Standard.A1.Flex` shape (2 OCPU/12GB, then retried at 1 OCPU/6GB) in Singapore West (AD-1). Both attempts failed with `Out of capacity for shape VM.Standard.A1.Flex in availability domain AD-1`. Confirmed as a known, widely-reported Always Free tier issue (Ampere capacity is shared and frequently exhausted), not an account or configuration problem. Checked "Specialty and previous generation" shapes for a smaller free x86 fallback (`VM.Standard.E2.1.Micro`) — not offered on this tenancy, confirming newer Oracle accounts only receive Ampere-based Always Free resources.

**Azure (fallback attempt):** Created a new Pay-As-You-Go Azure account same-day (selected over Free Trial specifically because Free Trial subscriptions cannot request quota increases). Verified B-series VM pricing/allowances (B1s: free for 12 months under 750 hrs/month, ~$7.59/month after) before committing. Hit an equivalent blocker: attempting to select `Standard_B1s` in Southeast Asia surfaced a `Request quota` requirement; using Azure's own quota-recommendation tool confirmed **B-series is unavailable in Southeast Asia entirely** (not just quota-limited) — a regional capacity constraint, not something a support ticket resolves. Alternative regions offered by Azure's recommender (East Asia) did not include compatible small-instance SKUs. Cancelled the Azure subscription same-day (confirmed stopped billing immediately; full resource deletion scheduled by Microsoft for on/before Oct 20, 2026) rather than pursue a region that breaks the original Southeast Asia + AZ-900 synergy plan.

**Decision:** pivot to DigitalOcean (paid, no free-tier capacity lottery) for Day 90 — chosen over Hetzner due to Hetzner's newly-confirmed biometric ID verification requirement (passport-tier document + live selfie), which poses real rejection risk given available ID documents on hand.

**No structural regret on Oracle/Azure attempts** — both were reasonable, well-reasoned choices at the time; the blockers were genuine provider-side capacity constraints affecting many users, not planning errors.

## About
Part of my transition into remote software engineering (QA Automation → SDET → Full-Stack).
Daily build-in-public log and full C# learning history: [github.com/dreckieee/csharp](https://github.com/dreckieee/csharp)

## How to Run Locally
run `dotnet run`
Then open `http://localhost:{port}/swagger` in your browser to explore the API.

Note: swap {port} to your local port — check your terminal output for the exact URL after running `dotnet run`.

To run tests: `dotnet test` from `VapeShopInventoryAPI.Tests` — no separate server needs to be running first; the test suite boots the API in-process against an isolated in-memory database.