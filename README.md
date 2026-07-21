# Vape Shop Inventory API

ASP.NET Core Web API for inventory management — built for a real Vape Shop business.

## Status: In Progress
Build 1 (Product CRUD) and Build 2 (Expense CRUD) complete and tested end-to-end, including unique SKU constraint, structured exception handling, and DTO-based update binding. Build 3 (Sale + SaleItem) is fully complete end-to-end: domain layer, EF Core migrations, DTOs, `SalesController` (Create, Get, EditSaleDate, CloseSale, CancelSale), and `SaleItemsController` (AddSaleItem, ReduceSaleItemQuantity) are all implemented and tested. The self-identified cancel-empty-sale gap (Day 79) is now closed. A Playwright + NUnit test project has been scaffolded (Day 83), and now includes 5 passing API-mode tests against this API's own `GET`/`POST` `/api/Products` endpoints. A dedicated Products DTO layer (`CreateProductRequest`/`ProductResponse`) was introduced Day 88, fixing a real bug where invalid input to `POST /api/Products` returned an unhandled `500` instead of a clean `400`. Deployment remains the only item blocking Step 2 completion — see Day 89 log for current hosting-provider status.

## Tech Stack
- .NET 10 / ASP.NET Core (Controllers)
- Entity Framework Core
- SQLite

## Testing
- `VapeShopInventoryAPI.Tests` — dedicated NUnit test project (Day 83), using `Microsoft.Playwright.NUnit` for both browser-based and API-mode test automation
- First smoke test (Day 83): navigates to a live page and asserts on page title, confirming the full pipeline (build → browser install → test execution) works end-to-end
- First API-mode test (Day 84): `ProductsApiTests.GetProducts_ReturnsSuccessAndNonEmptyList`
- Not-found test (Day 86): `ProductsApiTests.GetProduct_NonExistentId_ReturnsNotFound`
- Create test with cleanup (Day 87): `ProductsApiTests.CreateProduct_ValidProduct_ReturnsCreated`
- Invalid-payload test (Day 88): `ProductsApiTests.CreateProduct_InvalidProduct_ReturnsBadRequest`
- `[SetUp]`/`[TearDown]` refactor (Day 88): shared `IAPIRequestContext` creation/disposal across all Products tests
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
- Negative ids (e.g. `-1`) are a reliable choice for "guaranteed non-existent" test data

### Design decision: stock deduction timing
Stock is deducted from `Product.StockQuantity` at `CloseSale` time, not at `AddSaleItem` time. Built for a single-register, in-person retail context — not designed for multiple simultaneous registers against the same stock pool (accepted, documented tradeoff).

### Design decision: `CancelSale` scope and limitation
`CancelSale` permanently deletes an open sale and any attached `SaleItem` rows, uniformly regardless of item count, since nothing affects stock until `CloseSale` runs. No audit trail is kept for cancelled sales — acceptable since an unclosed sale was never a completed transaction.

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