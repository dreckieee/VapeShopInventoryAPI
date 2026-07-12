# Vape Shop Inventory API

ASP.NET Core Web API for inventory management — built for a real Vape Shop business.

## Status: In Progress
Build 1 (Product CRUD) and Build 2 (Expense CRUD) complete and tested end-to-end, including unique SKU constraint, structured exception handling, and DTO-based update binding. Build 3 (Sale + SaleItem) is now functionally complete end-to-end: domain layer, EF Core migrations, DTOs, `SalesController` (Create, Get, EditSaleDate, CloseSale), and `SaleItemsController` (AddSaleItem, ReduceSaleItemQuantity) are all implemented and tested.

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
  
- [x] Build 3 — Sale + SaleItem (1-to-many)
  - [x] `Sale` domain class — closed-sale guard, minimum-one-item close rule, item collection encapsulation
  - [x] `SaleItem` domain class — snapshot-at-creation `UnitPriceAtSale`, aggregate-root pattern (no self-`Edit()`)
  - [x] EF Core relationships — `Sale`↔`SaleItem` (restrict-on-delete) and `Product`↔`SaleItem` (restrict-on-delete), both migrated
  - [x] Blind recreation drill — domain logic reconstructed from memory, verified correct
  - [x] Combine-on-add — duplicate product+price additions merge into existing item's quantity instead of creating a new row
  - [x] `ReduceQuantity` — partial quantity reduction with guard against over-reduction; auto-removes item at zero quantity
  - [x] Audit counters — `TransactionCount` (per-sale item numbering), `ReductionFrequency`, `TotalQuantityReduction` for manager-facing anomaly review
  - [x] `AddSaleAuditFields` migration — added `TransactionNumber`/`TransactionCount`/`ReductionFrequency`/`TotalQuantityReduction` columns, DB wiped and re-migrated clean
  - [x] Request/response DTOs — `CreateSaleRequest`, `AddSaleItemRequest`, `ReduceSaleItemQuantityRequest`, `EditSaleDateRequest`, `SaleItemResponse`, `SaleResponse`, `StockShortageResponse`
  - [x] `SalesController` — `CreateSale`, `GetSale`, `EditSaleDate`, `CloseSale`
  - [x] `SaleItemsController` — `AddSaleItem` (with stock-availability check), `ReduceSaleItemQuantity`
  - [x] `CloseSale` endpoint — finalizes a sale, rechecks stock, and deducts on success

## Tech notes
- SQLite maps `decimal` → `TEXT` (exact precision, avoids float rounding vs REAL)
- No magic strings: table/column names resolved dynamically via `_context.Model`
- `Sale.SaleItems` navigation uses `UsePropertyAccessMode(PropertyAccessMode.Field)` since it's exposed as a computed `IReadOnlyList<SaleItem>`, not a settable property
- `SaleItem.TransactionNumber` is per-`Sale`, monotonically increasing (never reused, never decremented) — gaps from removed items are expected and preserved for audit purposes, not treated as errors
- Response DTOs (`SaleResponse`, `SaleItemResponse`, `StockShortageResponse`) never expose raw domain entities — avoids circular reference (`Sale` ↔ `SaleItem` navigation) and keeps internal fields out of the API contract
- Deferred: gapless `DisplayPosition` field on `SaleItemResponse` for clean receipt-style numbering (computed per-response, not stored)
- Controllers use `.Include()` (and `.ThenInclude()` where a nested navigation property like `SaleItem.Product` is needed) to eager-load related data when fetching a `Sale` — `FindAsync`/`FirstOrDefaultAsync` alone only loads the root entity, never related collections, unless explicitly told to
- `ReduceSaleItemQuantityRequest` deliberately omits a `SaleItemId` field — the item id is already carried in the route (`{itemId}`), so duplicating it in the body would create two sources of truth for the same value
- `Product.ReduceStock()` guards independently against over-reduction (`InvalidOperationException` if the amount requested exceeds current stock) — it does not trust callers to have already checked, so the guard holds even if future code calls it from a new call site
- `CloseSale()` collects **all** stock shortages across a sale's items before throwing, rather than failing on the first one found — a cashier fixing a multi-item sale sees every problem at once instead of one at a time across repeated close attempts

### Design decision: stock deduction timing
Stock is deducted from `Product.StockQuantity` at `CloseSale` time, not at `AddSaleItem` time. `AddSaleItem` does check current stock and rejects the request (`409 Conflict`) if insufficient, but does not deduct — it only prevents adding more than what's available at that moment. `CloseSale` rechecks stock for every item immediately before committing, since stock can change between `AddSaleItem` and `CloseSale`.

**Why:** this API is built for a single-register, in-person retail context — a sale is built and closed within minutes by one cashier, not held open across many concurrent shoppers like an e-commerce cart. Deducting stock only on close avoids needing to "reserve" stock for open/abandoned sales, which would otherwise require restore-on-remove/restore-on-abandon logic.

**Known limitation (explicit scope boundary):** this API assumes single-register, low-concurrency, in-person retail. It is **not** designed for shops running multiple simultaneous registers/cashiers against the same stock pool. Two registers could both add the last unit of a low-stock item to two different open sales before either closes — a race condition this API does not fully prevent. `CloseSale`'s stock recheck mitigates the worst outcome (no incorrect deduction ever happens — the close is rejected with a `409 Conflict` instead), but it does not eliminate the underlying race. Accepted tradeoff for the target use case (single physical shop, one register).

### Design decision: `CloseSale` failure handling
If one or more sale items fail the stock recheck at close time, `CloseSale` collects every failing item (not just the first) and throws a single exception carrying the full list — no partial state is ever saved (nothing is written to the database until the entire recheck passes and every item is confirmed decrementable). The sale remains open and untouched, and the cashier can address every reported shortage before retrying the close.

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

### Sales
- `POST /api/Sales` — create a new sale
- `GET /api/Sales/{id}` — get a sale with its items
- `PATCH /api/Sales/{id}/date` — edit the sale date (blocked if sale is closed; rejects default/future dates)
- `POST /api/Sales/{id}/close` — finalize a sale (`CloseSale`):
  - `400 BadRequest` if the sale is already closed, or has zero items
  - `409 Conflict` if one or more items now have insufficient stock — returns a list of every affected item:
```json
    [
      {
        "productId": 1,
        "productName": "RELX Mango Juice",
        "requestedQuantity": 3,
        "availableQuantity": 1
      }
    ]
```
    No stock is deducted and the sale stays open if any shortage is found.
  - `200 OK` with the updated `SaleResponse` on success — stock is decremented for every item and the sale is marked closed

### Sale Items
- `POST /api/Sales/{saleId}/items` — add an item to a sale (combines quantity if same product + unit price already exists on the sale; rejects if requested quantity exceeds current product stock)
- `PATCH /api/Sales/{saleId}/items/{itemId}/reduce` — reduce an item's quantity (auto-removes the item if reduced to zero; updates audit counters on the sale)

## Day 80 — Scoping (in progress)
Build 3 complete. Options for next session:
- Deploy the Web API (roadmap's Step 2 target)
- Fix cancel-empty-sale gap (self-identified Day 79)
- Deferred stretch items (DisplayPosition, audit log)
- Begin Blazor Server UI phase

## About
Part of my transition into remote software engineering (QA Automation → SDET → Full-Stack).
Daily build-in-public log and full C# learning history: [github.com/dreckieee/csharp](https://github.com/dreckieee/csharp)

## How to Run Locally
run `dotnet run`
Then open `http://localhost:{port}/swagger` in your browser to explore the API.

Note: swap {port} to your local port — check your terminal output for the exact URL after running `dotnet run`.