# Vape Shop Inventory API

ASP.NET Core Web API for inventory management — built for a real Vape Shop business.

## Status: In Progress
Build 1 (Product CRUD) and Build 2 (Expense CRUD) complete and tested end-to-end, including unique SKU constraint, structured exception handling, and DTO-based update binding. Build 3 (Sale + SaleItem) domain layer complete, including combine-on-add logic, per-sale transaction numbering, quantity reduction with auto-remove, and audit counters. EF Core migrations and DTO layer complete — controllers next.

## Tech Stack
- .NET 10 / ASP.NET Core (Controllers)
- Entity Framework Core
- SQLite

## Roadmap Checklist

- [x] Build 1 — Product CRUD (GET/POST/PUT/DELETE)
  - [x] Routing, Controllers, DI, EF Core basics, SQLite
  - [x] Unique SKU constraint + structured exception handling
  - [x] Swagger/OpenAPI UI

- [x] Build 2 — Expense CRUD (GET/POST/PUT/DELETE)
  - [x] Domain validation (Description, Amount, Category, Date)
  - [x] EF Core migration for Expenses table
  - [x] UpdateExpenseRequest DTO for PUT binding
  - [x] Tested end-to-end via curl.exe
  
- [ ] Build 3 — Sale + SaleItem (1-to-many)
  - [x] `Sale` domain class — closed-sale guard, minimum-one-item close rule, item collection encapsulation
  - [x] `SaleItem` domain class — snapshot-at-creation `UnitPriceAtSale`, aggregate-root pattern (no self-`Edit()`)
  - [x] EF Core relationships — `Sale`↔`SaleItem` (restrict-on-delete) and `Product`↔`SaleItem` (restrict-on-delete), both migrated
  - [x] Blind recreation drill — domain logic reconstructed from memory, verified correct
  - [x] Combine-on-add — duplicate product+price additions merge into existing item's quantity instead of creating a new row
  - [x] `ReduceQuantity` — partial quantity reduction with guard against over-reduction; auto-removes item at zero quantity
  - [x] Audit counters — `TransactionCount` (per-sale item numbering), `ReductionFrequency`, `TotalQuantityReduction` for manager-facing anomaly review
  - [x] `AddSaleAuditFields` migration — added `TransactionNumber`/`TransactionCount`/`ReductionFrequency`/`TotalQuantityReduction` columns, DB wiped and re-migrated clean
  - [x] Request/response DTOs — `CreateSaleRequest`, `AddSaleItemRequest`, `ReduceSaleItemQuantityRequest`, `SaleItemResponse`, `SaleResponse`
  - [ ] `SalesController` / `SaleItemsController` — CRUD + `CloseSale` endpoint

## Tech notes
- SQLite maps `decimal` → `TEXT` (exact precision, avoids float rounding vs REAL)
- No magic strings: table/column names resolved dynamically via `_context.Model`
- `Sale.SaleItems` navigation uses `UsePropertyAccessMode(PropertyAccessMode.Field)` since it's exposed as a computed `IReadOnlyList<SaleItem>`, not a settable property
- `SaleItem.TransactionNumber` is per-`Sale`, monotonically increasing (never reused, never decremented) — gaps from removed items are expected and preserved for audit purposes, not treated as errors
- Response DTOs (`SaleResponse`, `SaleItemResponse`) never expose raw domain entities — avoids circular reference (`Sale` ↔ `SaleItem` navigation) and keeps internal fields out of the API contract
- Deferred: gapless `DisplayPosition` field on `SaleItemResponse` for clean receipt-style numbering (computed per-response, not stored)

## Endpoints

### Products
- `GET /api/Products` — list all products
- `GET /api/Products/{id}` — get product by id
- `POST /api/Products` — create product
- `PUT /api/Products/{id}` — update product
- `DELETE /api/Products/{id}` — delete product

### Expenses
- `GET /api/Expenses` — list all expenses
- `GET /api/Expenses/{id}` — get expense by id
- `POST /api/Expenses` — create expense
- `PUT /api/Expenses/{id}` — update expense
- `DELETE /api/Expenses/{id}` — delete expense

### Sales (in progress)
- Endpoints not yet implemented — domain layer, EF Core migrations, and DTOs complete, controllers next session

## About
Part of my transition into remote software engineering (QA Automation → SDET → Full-Stack).
Daily build-in-public log and full C# learning history: [github.com/dreckieee/csharp](https://github.com/dreckieee/csharp)

## How to Run Locally
run `dotnet run`
Then open `http://localhost:{port}/swagger` in your browser to explore the API.

Note: swap {port} to your local port — check your terminal output for the exact URL after running `dotnet run`.