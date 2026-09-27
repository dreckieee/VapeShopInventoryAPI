# Vape Shop Inventory API

ASP.NET Core Web API for inventory management — built for a real Vape Shop business. 

## Status: Deployed
Product CRUD, Expense CRUD, full Sale/SaleItem lifecycle (create, add/reduce items, close, cancel), monthly filtering on Sales/Expenses, a computed income endpoint, and a batch restock endpoint with itemized delivery cost tracking (`DeliveryItem`) are complete and deployed. `PaymentMethod`/`PaymentNote` tracking on Sales and Expenses is live in production as of Day 133 — enum values serialize as strings (e.g. `"Cash"`) in all API responses and accept either string or int form on input. The Settlement feature (recording payments against outstanding Receivable/Payable balances) is feature-complete, fully tested, and deployed to production as of Day 141: create/get/list endpoints, all guards (target validation, closed-sale requirement, overpayment prevention, settlement date-validity), computed `AmountSettled`/`OutstandingBalance`/`TotalAmount` fields on Sale and Expense responses, and the PaymentMethod lock guard (once any settlement exists against a record, its PaymentMethod becomes permanently uneditable) are all implemented and under direct automated test coverage. A `CapitalTransaction` entity (tracking owner deposits/withdrawals separately from Expense) was added Day 142 and **deployed to production as of Day 145**, with full create/get/list endpoints and automated test coverage. A `CashBalanceCalculator`/`GET /api/cashbalance` endpoint (four running balances: Cash on Hand, Digital Balance, Receivables Outstanding, Payables Outstanding) is complete, fully tested, and **deployed to production as of Day 145**. Day 144 found and fixed a real semantic bug in `SettlementCalculator`: Cash/DigitalPayment Sales and Expenses were being reported as fully unpaid (`OutstandingBalance` = full amount) when they're always paid in full at creation and can never have a Settlement — fixed with a short-circuit returning `(0, fullAmount)` for those records, and **deployed to production as of Day 145**. A `paymentMethod` filter was added to `GET /api/Sales` and `GET /api/Expenses` on Day 145 — built, tested, and deployed same day — making it easy to find outstanding Receivable/Payable records without paging through the full list. 102 automated tests via `WebApplicationFactory` against an isolated in-memory database cover Product, Sale/SaleItem, Restock, Expense, Settlement, CapitalTransaction, and Cash Balance flows, plus manual verification against the live deployed instance. Live on a DigitalOcean droplet as of Day 94, most recently redeployed Day 145.

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
- `GET /api/Expenses` — list expenses, optionally filtered by `?year=`, `?month=`, and/or `?paymentMethod=`; filters are independent and can be combined
- `GET /api/Expenses/{id}` — get expense by id
- `POST /api/Expenses` — create expense (includes `PaymentMethod`, required; `PaymentNote`, optional)
- `PUT /api/Expenses/{id}` — update expense (includes `PaymentMethod`/`PaymentNote`; returns `409 Conflict` if the expense is linked to a restock delivery and the caller attempts to change `Amount` or `Category`; returns `409 Conflict` if the expense has an existing settlement and the caller attempts to change `PaymentMethod`; returns `409 Conflict` if the expense has an existing settlement and the caller attempts to reduce `Amount` below the total already settled; returns `409 Conflict` if the expense has an existing settlement and the caller attempts to edit `Date` later than the earliest settlement date)
- `DELETE /api/Expenses/{id}` — delete expense (returns `409 Conflict` if the expense has existing delivery item references, or if the expense has existing settlement references)

Every `ExpenseResponse` includes computed `AmountSettled` and `OutstandingBalance` fields — always `0`/full-`Amount` for Cash/DigitalPayment records (which can never have a Settlement), computed from actual settlements for Receivable/Payable records.

### Sales
- `GET /api/Sales` — list sales, optionally filtered by `?year=`, `?month=`, `?isClosed=`, and/or `?paymentMethod=`; filters are independent and can be combined (omitting a filter doesn't restrict on that axis)
- `POST /api/Sales` — create a new sale (includes `PaymentMethod`, required; `PaymentNote`, optional)
- `GET /api/Sales/{id}` — get a sale with its items
- `PUT /api/Sales/{id}` — edit sale date, payment method, and payment note (returns `409 Conflict` if the sale has an existing settlement and the caller attempts to change `PaymentMethod`; note that a closed sale — the only state a settlement can target — rejects all edits regardless of PaymentMethod, so this guard's `409` path is reachable but its would-be success path on a settled sale is not)
- `POST /api/Sales/{id}/close` — finalize a sale, decrementing stock
- `PUT /api/Sales/{id}/cancel` — permanently cancel an open sale

Every `SaleResponse` includes computed `TotalAmount`, `AmountSettled`, and `OutstandingBalance` fields — always `0`/full-`TotalAmount` for Cash/DigitalPayment records, computed from actual settlements for Receivable/Payable records.

### Sale Items
- `POST /api/Sales/{saleId}/items` — add an item to a sale (returns `400 BadRequest` if the sale is already closed)
- `PATCH /api/Sales/{saleId}/items/{itemId}/reduce` — reduce an item's quantity

### Income
- `GET /api/income` — computed revenue from closed sales; `?year=` and `?month=` are both optional and independently applicable (neither = all-time cumulative total, year only = whole year, both = specific month)

### Restock
- `POST /api/restock` — batch endpoint for recording a delivery: line items (product, quantity, unit cost) roll up into one `Expense` (category: `Restock`, amount computed from line costs, includes `PaymentMethod`/`PaymentNote`) and itemized `DeliveryItem` records per line, while updating stock for each product.

### Settlements
- `POST /api/Settlements` — record a payment against a Sale or Expense currently marked `Receivable`/`Payable` respectively. Requires exactly one of `SaleId`/`ExpenseId`, a positive `Amount`, and a settlement `PaymentMethod` of `Cash` or `DigitalPayment` (not `Receivable`/`Payable` — those describe the target's state, not the settlement itself). Returns `400 BadRequest` if: the target record doesn't exist, isn't in the correct Receivable/Payable state, is a Sale that hasn't been closed yet, is dated earlier than the target's own Sale/Expense date (same-day is allowed), or if `Amount` exceeds the target's outstanding balance (total minus previously settled amounts).
- `GET /api/Settlements` — list settlements, optionally filtered by `?year=` and/or `?month=`.
- `GET /api/Settlements/{id}` — get a settlement by id.
- Settlements are immutable by design — no edit or delete endpoint exists or is planned.
- Once any settlement exists against a Sale or Expense, that record's `PaymentMethod` becomes permanently locked — any attempt to change it via `PUT /api/Sales/{id}` or `PUT /api/Expenses/{id}` returns `409 Conflict`. On the Expense side, `Amount` also becomes floor-locked at the already-settled total, and `Date` becomes ceiling-locked at the earliest settlement's date — both can still be edited, but not past those bounds.

### Capital Transactions
- `POST /api/CapitalTransactions` — record an owner deposit or withdrawal of cash/digital funds into or out of the business, independent of Sales/Expenses. Requires `Type` (`Deposit`/`Withdrawal`), a positive `Amount`, a `PaymentMethod` of `Cash` or `DigitalPayment` only, and a `Date` (rejected if in the future). `PaymentNote` is optional.
- `GET /api/CapitalTransactions` — list capital transactions, optionally filtered by `?year=` and/or `?month=`.
- `GET /api/CapitalTransactions/{id}` — get a capital transaction by id.
- Capital transactions are immutable by design — no edit or delete endpoint, same rationale as Settlements.
- Kept entirely separate from `Expense` so that owner draws/contributions never distort the Income endpoint's revenue calculation.

### Cash Balance
- `GET /api/cashbalance` — returns four running balances: `CashOnHand`, `DigitalBalance`, `ReceivablesOutstanding`, `PayablesOutstanding`. Cumulative/all-time by default — no date filtering (a running balance, not a period-scoped figure). `CashOnHand`/`DigitalBalance` factor in Sales, Expenses, Settlements, and CapitalTransactions of the matching PaymentMethod; `ReceivablesOutstanding`/`PayablesOutstanding` reflect outstanding balances on open Receivable/Payable Sales and Expenses.

## Testing
`VapeShopInventoryAPI.Tests` — 102 NUnit tests (Product, Sale/SaleItem, Restock, Expense, Settlement, CapitalTransaction, and Cash Balance coverage) using `WebApplicationFactory<Program>` against an isolated in-memory SQLite database. Run with `dotnet test` from `VapeShopInventoryAPI.Tests` — no separate server needs to be running first. Restock coverage is partial: valid single/multi-product and invalid-ProductId cases are covered; duplicate-ProductId, invalid quantity/cost, and empty-Items cases are still open. `ExpensesApiTests` covers Create (valid/invalid, incl. `PaymentMethod` enum guard), computed-field correctness across zero/partial/full settlement states (incl. the Cash/DigitalPayment short-circuit fix), Get (existing/non-existent), Update (valid, invalid, non-existent-id, restock-reference-conflict, `PaymentMethod` enum guard, amount-below-settled guard), Delete (valid, non-existent-id, restock-reference-conflict, settlement-reference-conflict), and list filters on `GetExpenses` (year, month, year+month, `paymentMethod`, `paymentMethod`+year combined, no-match). `SalesApiTests` covers Create (valid, incl. `PaymentMethod` enum guard), computed-field correctness across zero/partial/full settlement states (incl. the Cash/DigitalPayment short-circuit fix), Get, Edit (incl. `PaymentMethod` enum guard), AddSaleItem (valid, and rejected on a closed sale), ReduceSaleItemQuantity (partial reduction, and reduction to zero removing the item), CloseSale (verifying stock deduction against a fresh product fetch), and list filters on `GetSales` (`paymentMethod`, `paymentMethod`+year combined, no-match). `PaymentMethod` enum guard coverage is complete on Sale, Expense, and CapitalTransaction paths for invalid-int values; invalid-string enum coverage (a structurally different model-binding failure path) is not yet written. `SettlementsApiTests` covers: valid settlement against a Receivable (closed) sale and a Payable expense, non-existent target id (Sale and Expense), target-not-Receivable/Payable (Sale and Expense), target-not-closed (Sale), settlement date earlier than the target's date (Sale and Expense, `400`) and the same-day boundary case (`201`), overpayment (Amount exceeds outstanding balance), entity-guard passthrough cases (neither FK set, both FKs set, non-positive amount, invalid PaymentMethod, future date), the `UpdateExpense` Date ceiling-lock guard (`409` when moved later than the earliest settlement, `200` on the same-day boundary), and full PaymentMethod lock guard coverage on both `EditSale`/`UpdateExpense`. `CapitalTransactionsApiTests` (17 tests) covers Create (valid Deposit/Withdrawal; invalid negative/zero Amount, Receivable/Payable/undefined PaymentMethod, undefined Type, default/future Date), Get (existing/non-existent id), and list filters (year, month, year+month, no-match, no-filter). `CashBalanceCalculatorTests` covers the full six-term formula (Sales, Expenses, Sale Settlements, Expense Settlements, Deposits, Withdrawals) across Cash and Digital buckets, at both the calculator level and the `GET /api/CashBalance` endpoint level, using an order-independent before/after delta assertion pattern.

## About
Part of my transition into remote software engineering (QA Automation → SDET → Full-Stack).
Daily build-in-public log: [github.com/dreckieee/csharp](https://github.com/dreckieee/csharp)

## How to Run Locally
`dotnet run`, then open `http://localhost:{port}/swagger` (check terminal output for the exact port).