# Vape Shop Inventory API

ASP.NET Core Web API for inventory management — built for a real Vape Shop business.

## Status: In Progress
Build 1 (Product CRUD) and Build 2 (Expense CRUD) complete and tested end-to-end, including unique SKU constraint, structured exception handling, and DTO-based update binding. Build 3 (Sale + SaleItem) is fully complete end-to-end: domain layer, EF Core migrations, DTOs, `SalesController` (Create, Get, EditSaleDate, CloseSale, CancelSale), and `SaleItemsController` (AddSaleItem, ReduceSaleItemQuantity) are all implemented and tested. The self-identified cancel-empty-sale gap (Day 79) is now closed. A Playwright + NUnit test project has been scaffolded (Day 83), and now includes 7 passing API-mode tests: 4 against `/api/Products`, 3 against `/api/Sales`. All response DTOs (`ProductResponse`, `SaleResponse`, `SaleItemResponse`) now use an `init`-only-property + static factory pattern (Day 91), superseding the private-constructor approach from Day 90 — see Day 91 log below. Deployment remains the only item blocking Step 2 completion — see Day 89 log for current hosting-provider status.

## Tech Stack
- .NET 10 / ASP.NET Core (Controllers)
- Entity Framework Core
- SQLite

## Testing
- `VapeShopInventoryAPI.Tests` — dedicated NUnit test project (Day 83), using `Microsoft.Playwright.NUnit` for API-mode test automation
- **Products (4 tests):** `GetProducts_ReturnsSuccessAndNonEmptyList` (Day 84), `GetProduct_NonExistentId_ReturnsNotFound` (Day 86), `CreateProduct_ValidProduct_ReturnsCreated` (Day 87, with cleanup), `CreateProduct_InvalidProduct_ReturnsBadRequest` (Day 88)
- **Sales (3 tests, added Day 91):** `GetSale_NonExistentId_ReturnsNotFound`, `CreateSale_ValidSaleRequest_ReturnsCreated`, `GetSale_ExistingId_ReturnsOk` — all sharing a private `CreateTestSaleAsync(DateTime saleDate)` helper that returns a named tuple `(IAPIResponse Response, SaleResponse? Sale)`, avoiding duplicated POST+deserialize logic across tests
- `[SetUp]`/`[TearDown]` refactor (Day 88): shared `IAPIRequestContext` creation/disposal across all Products tests; same pattern applied to Sales tests (Day 91), cleaning up via `PUT /api/Sales/{id}/cancel` since no plain delete endpoint exists
- Day 90: tests updated to deserialize responses into `ProductResponse` (not the raw `Product` entity), matching the tightened DTO construction rules
- Day 90: removed leftover default Playwright scaffold test (`Tests.cs`) that was causing a false failure on every run
- **Known gap (Day 91):** no test coverage yet for `CloseSale`, `AddSaleItem`, or `ReduceSaleItemQuantity`. Closed sales cannot be removed via the API (`CancelSale` only works on open sales, by design — see Design decision below), so tests exercising `CloseSale` would leave permanent rows in the database under the current test setup. Resolving this requires test-database isolation (in progress — see Day 91 log)
- Purpose: groundwork for Step 3 of the roadmap (Playwright + NUnit portfolio item) — future tests will target this API's own endpoints once deployed, in addition to local runs

## Roadmap Checklist

- [x] Build 1 — Product CRUD (GET/POST/PUT/DELETE)
- [x] Build 2 — Expense CRUD (GET/POST/PUT/DELETE)
- [x] Build 3 — Sale + SaleItem (1-to-many)
- [ ] Deployment (in progress — see Day 89 log)

## Tech notes
- SQLite maps `decimal` → `TEXT` (exact precision, avoids float rounding vs REAL)
- No magic strings: table/column names resolved dynamically via `_context.Model`
- Response DTOs never expose raw domain entities — avoids circular reference and keeps internal fields out of the API contract
- API responses serialize with camelCase JSON property names; deserializing directly into domain classes requires `PropertyNameCaseInsensitive = true`
- Negative ids (e.g. `-1`) are a reliable choice for "guaranteed non-existent" test data — auto-incrementing primary keys never produce negative values regardless of database state
- **Response DTO construction (revised Day 91):** response DTOs mapped from a domain entity (`ProductResponse`, `SaleResponse`, `SaleItemResponse`) are `record` types with `init`-only properties, plus a static factory method (`FromProduct`, `FromSale`, `FromSaleItem`) for mapping logic. This replaces the Day 90 private-constructor + `[JsonConstructor]` approach, which required extra plumbing to work with `System.Text.Json` and offered no real benefit for a DTO with no invariants to protect. `init` properties deserialize natively with no custom constructor needed; the factory method remains as a convenience for mapping, not as the sole construction path. Using `record` (rather than a plain `class`) also gives free value-equality, useful for test assertions comparing expected vs. actual deserialized responses. Input DTOs (`CreateProductRequest`, `UpdateProductRequest`, `CreateSaleRequest`, etc.) are unaffected — they keep public setters, since framework model-binding needs to construct them externally and they have no invariant to protect
- `SaleResponse.FromSale` maps its nested `SaleItems` collection via `.Select(SaleItemResponse.FromSaleItem).ToList()`, reusing `SaleItemResponse`'s own factory rather than duplicating item-mapping logic

### Design decision: stock deduction timing
Stock is deducted from `Product.StockQuantity` at `CloseSale` time, not at `AddSaleItem` time. Built for a single-register, in-person retail context — not designed for multiple simultaneous registers against the same stock pool (accepted, documented tradeoff).

### Design decision: `CancelSale` scope and limitation
`CancelSale` permanently deletes an open sale and any attached `SaleItem` rows, uniformly regardless of item count, since nothing affects stock until `CloseSale` runs. No audit trail is kept for cancelled sales — acceptable since an unclosed sale was never a completed transaction. Once a sale is closed, it cannot be deleted or cancelled through the API by design — a closed sale represents a completed transaction, not something any API consumer should be able to remove.

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

## Day 91 — DTO pattern revision (record + init); SalesApiTests added; test-DB isolation in progress

**DTO pattern correction:** the Day 90 private-constructor + `[JsonConstructor]` pattern was reconsidered as over-engineered for flat response DTOs with no real invariant to protect — the need for `[JsonConstructor]` was itself a signal the pattern fought `System.Text.Json`'s natural model. Revised all three response DTOs:
- `ProductResponse`, `SaleResponse`, `SaleItemResponse` converted to `record` types with `init`-only properties, no custom constructor
- `[JsonConstructor]` and the private constructor removed from `ProductResponse`
- Each DTO retains a static factory method (`FromProduct`, `FromSale`, `FromSaleItem`) for mapping — no longer the sole construction path, just a mapping convenience
- `SaleResponse.FromSale` maps nested `SaleItems` via `SaleItemResponse.FromSaleItem`, avoiding duplicated item-mapping logic
- Verified via `dotnet test`: all 4 `ProductsApiTests` still pass unchanged (test file needed no modification — `JsonSerializer.Deserialize<T>` doesn't depend on a DTO's internal construction mechanism)

**Controller wiring:** replaced all duplicated inline DTO construction with calls to the new factory methods:
- `SaleItemsController`: `AddSaleItem`, `ReduceSaleItemQuantity` now both return `SaleResponse.FromSale(sale)`
- `SalesController`: `GetSale`, `CreateSale`, `CloseSale`, `EditSaleDate` now all return `SaleResponse.FromSale(sale)`

**New test coverage:** added `SalesApiTests.cs` with 3 tests (`GetSale_NonExistentId_ReturnsNotFound`, `CreateSale_ValidSaleRequest_ReturnsCreated`, `GetSale_ExistingId_ReturnsOk`), following the same `[SetUp]`/`[TearDown]` pattern as `ProductsApiTests`. Cleanup uses `PUT /api/Sales/{id}/cancel` (no plain delete endpoint exists by design — see Design decision above). A shared private helper `CreateTestSaleAsync(DateTime saleDate)` returns a named tuple `(IAPIResponse Response, SaleResponse? Sale)`, eliminating duplicated POST+deserialize code across tests. All 7 tests (4 Products + 3 Sales) pass via `dotnet test`.

**Self-identified gap:** no coverage yet for `CloseSale`, `AddSaleItem`, or `ReduceSaleItemQuantity`. Since `CancelSale` only works on open sales (by design), any test that closes a sale would leave a permanent, un-removable row in the database under the current test setup — closed sales aren't currently deletable via the API. Solving this properly requires test-database isolation, rather than reaching around the API or accepting permanent test data.

**In progress:** adopting `WebApplicationFactory<Program>` for in-process integration testing, replacing the current manual `dotnet run` + Playwright-against-`localhost` setup. This will let the test run swap in a dedicated test database via DI overrides, removing both the closed-sale cleanup problem and the manual-server-start dependency (also a prerequisite for running this test suite unattended in GitHub Actions CI/CD, per the roadmap's non-optional CI/CD requirement). First step complete: `Program.cs` now declares `public partial class Program { }` so the test project can reference the app's entry point.

**Deployment status:** still deferred pending the above test-isolation work — DigitalOcean account creation and Droplet provisioning to follow once `WebApplicationFactory` lands.

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