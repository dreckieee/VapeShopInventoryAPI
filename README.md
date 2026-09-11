# Vape Shop Inventory API

ASP.NET Core Web API for inventory management — built for a real Vape Shop business. 

## Status: Deployed (Settlement feature complete, not yet deployed)
Product CRUD, Expense CRUD, full Sale/SaleItem lifecycle (create, add/reduce items, close, cancel), monthly filtering on Sales/Expenses, a computed income endpoint, and a batch restock endpoint with itemized delivery cost tracking (`DeliveryItem`) are complete and deployed. `PaymentMethod`/`PaymentNote` tracking on Sales and Expenses is live in production as of Day 133 — enum values serialize as strings (e.g. `"Cash"`) in all API responses and accept either string or int form on input. The Settlement feature (recording payments against outstanding Receivable/Payable balances) is feature-complete as of Day 138: create/get endpoints, all guards (target validation, closed-sale requirement, overpayment prevention), computed `AmountSettled`/`OutstandingBalance`/`TotalAmount` fields on Sale and Expense responses, and the PaymentMethod lock guard (once any settlement exists against a record, its PaymentMethod becomes permanently uneditable) are all implemented, and as of Day 139 all of it is under direct automated test coverage — the feature has not yet been deployed to production. Day 139 also found and fixed two real bugs surfaced while writing that coverage: `DeleteExpense` was returning a raw `500` instead of a `409 Conflict` when a Settlement referenced the expense, and `UpdateExpense` was allowing an expense's `Amount` to be edited below its already-settled total, which could produce a nonsensical negative `OutstandingBalance`. Both are now guarded and tested. 62 automated tests via `WebApplicationFactory` against an isolated in-memory database cover Product, Sale/SaleItem, Restock, Expense, and Settlement flows, plus manual verification against the live deployed instance. Live on a DigitalOcean droplet as of Day 94, most recently redeployed Day 133.

## Tech Stack
- .NET 10 / ASP.NET Core (Controllers)
- Entity Framework Core
- SQLite

## Live Deployment
- Hosted on a DigitalOcean droplet (Singapore region), running as a systemd service on Ubuntu 24.04 LTS
- Swagger UI is deliberately enabled in Production for demo accessibility (not standard practice for a real production API)
- **No authentication is implemented** — a deliberate scope choice for this portfolio demo, not an oversight. A production deployment would require auth before any public write access
- The live URL is intentionally not published in this README — used for direct demos (e.g. interviews) rather than left publicly discoverable, given the lack of authentication

See [DECISIONS.md](./DECISIONS.md) for design rationale, known issues, and deployment history.

## Endpoints

### Products
- `GET /api/Products` — list products, optionally filtered by `?name=` (case-insensitive partial match) and/or `?category=` (case-insensitive partial match); filters are independent and can be combined
- `GET /api/Products/{id}` — get product by id
- `POST /api/Products` — create product (returns `400 BadRequest` on invalid input)
- `PUT /api/Products/{id}` — update product
- `DELETE /api/Products/{id}` — delete product (returns `409 Conflict` if the product has existing sale item or delivery item references)

### Expenses
- `GET /api/Expenses` — list expenses, optionally filtered by `?year=` and/or `?month=`
- `GET /api/Expenses/{id}` — get expense by id
- `POST /api/Expenses` — create expense (includes `PaymentMethod`, required; `PaymentNote`, optional)
- `PUT /api/Expenses/{id}` — update expense (includes `PaymentMethod`/`PaymentNote`; returns `409 Conflict` if the expense is linked to a restock delivery and the caller attempts to change `Amount` or `Category`; returns `409 Conflict` if the expense has an existing settlement and the caller attempts to change `PaymentMethod`; returns `409 Conflict` if the expense has an existing settlement and the caller attempts to reduce `Amount` below the total already settled)
- `DELETE /api/Expenses/{id}` — delete expense (returns `409 Conflict` if the expense has existing delivery item references, or if the expense has existing settlement references)

Every `ExpenseResponse` now includes computed `AmountSettled` and `OutstandingBalance` fields (always present, `0`/full-`Amount` for non-Receivable/Payable records).

### Sales
- `GET /api/Sales` — list sales, optionally filtered by `?year=`, `?month=`, and/or `?isClosed=`; filters are independent and can be combined (omitting a filter doesn't restrict on that axis)
- `POST /api/Sales` — create a new sale (includes `PaymentMethod`, required; `PaymentNote`, optional)
- `GET /api/Sales/{id}` — get a sale with its items
- `PUT /api/Sales/{id}` — edit sale date, payment method, and payment note (route/verb changed from `PATCH .../edit` for full-replacement consistency with Expenses; returns `409 Conflict` if the sale has an existing settlement and the caller attempts to change `PaymentMethod`)
- `POST /api/Sales/{id}/close` — finalize a sale, decrementing stock
- `PUT /api/Sales/{id}/cancel` — permanently cancel an open sale

Every `SaleResponse` now includes computed `TotalAmount`, `AmountSettled`, and `OutstandingBalance` fields (always present, `0`/full-`TotalAmount` for non-Receivable/Payable records).

### Sale Items
- `POST /api/Sales/{saleId}/items` — add an item to a sale (returns `400 BadRequest` if the sale is already closed)
- `PATCH /api/Sales/{saleId}/items/{itemId}/reduce` — reduce an item's quantity

### Income
- `GET /api/income` — computed revenue from closed sales; `?year=` and `?month=` are both optional and independently applicable (neither = all-time cumulative total, year only = whole year, both = specific month)

### Restock
- `POST /api/restock` — batch endpoint for recording a delivery: line items (product, quantity, unit cost) roll up into one `Expense` (category: `Restock`, amount computed from line costs, includes `PaymentMethod`/`PaymentNote`) and itemized `DeliveryItem` records per line, while updating stock for each product.

### Settlements (complete, not yet deployed to production)
- `POST /api/Settlements` — record a payment against a Sale or Expense currently marked `Receivable`/`Payable` respectively. Requires exactly one of `SaleId`/`ExpenseId`, a positive `Amount`, and a settlement `PaymentMethod` of `Cash` or `DigitalPayment` (not `Receivable`/`Payable` — those describe the target's state, not the settlement itself). Returns `400 BadRequest` if: the target record doesn't exist, isn't in the correct Receivable/Payable state, is a Sale that hasn't been closed yet, or if `Amount` exceeds the target's outstanding balance (total minus previously settled amounts).
- `GET /api/Settlements/{id}` — get a settlement by id.
- Settlements are immutable by design — no edit or delete endpoint exists or is planned.
- Once any settlement exists against a Sale or Expense, that record's `PaymentMethod` becomes permanently locked — any attempt to change it via `PUT /api/Sales/{id}` or `PUT /api/Expenses/{id}` returns `409 Conflict`. On the Expense side, `Amount` also becomes floor-locked at the already-settled total — it can still be edited, but not reduced below what's been settled.

## Testing
`VapeShopInventoryAPI.Tests` — 62 NUnit tests (Product, Sale/SaleItem, Restock, Expense, and Settlement coverage) using `WebApplicationFactory<Program>` against an isolated in-memory SQLite database. Run with `dotnet test` from `VapeShopInventoryAPI.Tests` — no separate server needs to be running first. Restock coverage is partial: valid single/multi-product and invalid-ProductId cases are covered; duplicate-ProductId, invalid quantity/cost, and empty-Items cases are still open. `ExpensesApiTests` covers Create (valid/invalid, incl. `PaymentMethod` enum guard), computed-field correctness across zero/partial/full settlement states, Get (existing/non-existent), Update (valid, invalid, non-existent-id, restock-reference-conflict, `PaymentMethod` enum guard, amount-below-settled guard), Delete (valid, non-existent-id, restock-reference-conflict, settlement-reference-conflict), and list filters on `GetExpenses` (filter by year, filter by month, filter by year+month composability, no-matches-returns-empty-list). `SalesApiTests` covers Create (valid, incl. `PaymentMethod` enum guard), computed-field correctness across zero/partial/full settlement states, Get, Edit (incl. `PaymentMethod` enum guard), AddSaleItem (valid, and rejected on a closed sale), ReduceSaleItemQuantity (partial reduction, and reduction to zero removing the item), and CloseSale (verifying stock deduction against a fresh product fetch). `PaymentMethod` enum guard coverage is complete on both Sale and Expense paths for invalid-int values; invalid-string enum coverage (a structurally different model-binding failure path) is not yet written. `SettlementsApiTests` covers: valid settlement against a Receivable (closed) sale and a Payable expense, non-existent target id (Sale and Expense), target-not-Receivable/Payable (Sale and Expense), target-not-closed (Sale), overpayment (Amount exceeds outstanding balance), and entity-guard passthrough cases (neither FK set, both FKs set, non-positive amount, invalid PaymentMethod, future date). Not yet covered: dedicated assertions on the PaymentMethod lock guard on `EditSale`/`UpdateExpense` (implemented and manually verified, computed-field and amount-guard coverage is complete as of Day 139, but the lock guard itself has no direct test yet).

## About
Part of my transition into remote software engineering (QA Automation → SDET → Full-Stack).
Daily build-in-public log: [github.com/dreckieee/csharp](https://github.com/dreckieee/csharp)

## How to Run Locally
`dotnet run`, then open `http://localhost:{port}/swagger` (check terminal output for the exact port).