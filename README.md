# Vape Shop Inventory API

ASP.NET Core Web API for inventory management — built for a real Vape Shop business.

## Status: Deployed
Product CRUD, Expense CRUD, full Sale/SaleItem lifecycle (create, add/reduce items, close, cancel), monthly filtering on Sales/Expenses, a computed income endpoint, and a batch restock endpoint with itemized delivery cost tracking (`DeliveryItem`) are complete and deployed. `PaymentMethod`/`PaymentNote` tracking on Sales and Expenses is code-complete, with the database migration created and applied locally (reconfirmed on a new dev environment as of Day 112) — not yet deployed to production, pending final test coverage. 23 automated tests via `WebApplicationFactory` against an isolated in-memory database cover Product, Sale/SaleItem, Restock, and Expense flows, plus manual verification against the live deployed instance. Live on a DigitalOcean droplet as of Day 94, most recently redeployed Day 106.

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
- `DELETE /api/Products/{id}` — delete product (returns `409 Conflict` if the product has existing sale item references)

### Expenses
- `GET /api/Expenses` — list expenses, optionally filtered by `?year=` and/or `?month=`
- `GET /api/Expenses/{id}` — get expense by id
- `POST /api/Expenses` — create expense (includes `PaymentMethod`, required; `PaymentNote`, optional — not yet deployed)
- `PUT /api/Expenses/{id}` — update expense (includes `PaymentMethod`/`PaymentNote` — not yet deployed)
- `DELETE /api/Expenses/{id}` — delete expense (returns `409 Conflict` if the expense has existing delivery item references)

### Sales
- `GET /api/Sales` — list sales, optionally filtered by `?year=`, `?month=`, and/or `?isClosed=`; filters are independent and can be combined (omitting a filter doesn't restrict on that axis)
- `POST /api/Sales` — create a new sale (includes `PaymentMethod`, required; `PaymentNote`, optional — not yet deployed)
- `GET /api/Sales/{id}` — get a sale with its items
- `PUT /api/Sales/{id}` — edit sale date, payment method, and payment note (route/verb changed from `PATCH .../edit` for full-replacement consistency with Expenses — `PaymentMethod`/`PaymentNote` fields not yet deployed)
- `POST /api/Sales/{id}/close` — finalize a sale, decrementing stock
- `PUT /api/Sales/{id}/cancel` — permanently cancel an open sale

### Sale Items
- `POST /api/Sales/{saleId}/items` — add an item to a sale
- `PATCH /api/Sales/{saleId}/items/{itemId}/reduce` — reduce an item's quantity

### Income
- `GET /api/income` — computed revenue from closed sales; `?year=` and `?month=` are both optional and independently applicable (neither = all-time cumulative total, year only = whole year, both = specific month)

### Restock
- `POST /api/restock` — batch endpoint for recording a delivery: line items (product, quantity, unit cost) roll up into one `Expense` (category: `Restock`, amount computed from line costs, includes `PaymentMethod`/`PaymentNote` once deployed) and itemized `DeliveryItem` records per line, while updating stock for each product.

## Testing
`VapeShopInventoryAPI.Tests` — 23 NUnit tests (Product, Sale/SaleItem, Restock, and Expense coverage) using `WebApplicationFactory<Program>` against an isolated in-memory SQLite database. Run with `dotnet test` from `VapeShopInventoryAPI.Tests` — no separate server needs to be running first. Restock coverage is partial: valid single/multi-product and invalid-ProductId cases are covered; duplicate-ProductId, invalid quantity/cost, and empty-Items cases are still open. `ExpensesApiTests` is in progress: Create (valid/invalid) is covered; Get, Update/Delete, list filters, and full `PaymentMethod` enum guard coverage are still open. `PaymentMethod` enum guard coverage on the Sale path has not been added yet either — queued after Expense path is complete.

## About
Part of my transition into remote software engineering (QA Automation → SDET → Full-Stack).
Daily build-in-public log: [github.com/dreckieee/csharp](https://github.com/dreckieee/csharp)

## How to Run Locally
`dotnet run`, then open `http://localhost:{port}/swagger` (check terminal output for the exact port).