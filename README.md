# Vape Shop Inventory API

ASP.NET Core Web API for inventory management — built for a real Vape Shop business.

## Status: In Progress
Build 1 (Product CRUD) complete and tested end-to-end, including unique SKU constraint and structured exception handling. Starting Build 2 (Expense CRUD) next.

## Tech Stack
- .NET 10 / ASP.NET Core (Controllers)
- Entity Framework Core
- SQLite

## Roadmap
- [x] Product domain model
- [x] DbContext setup
- [x] Product CRUD endpoints (GET/POST/PUT/DELETE)
- [x] Unique SKU constraint (Fluent API index)
- [x] Structured exception handling (409 Conflict on duplicate SKU, ILogger integration)
- [x] Swagger/OpenAPI UI
- [ ] Expense CRUD
- [ ] Sale + SaleItem (1-to-many relationships, transactional logic)

## Tech notes
- SQLite maps `decimal` → `TEXT` (exact precision, avoids float rounding vs REAL)
- No magic strings: table/column names resolved dynamically via `_context.Model`

## About
Part of my transition into remote software engineering (QA Automation → SDET → Full-Stack).
Daily build-in-public log and full C# learning history: [github.com/dreckieee/csharp](https://github.com/dreckieee/csharp)

## How to Run Locally
run `dotnet run`
Then open `http://localhost:{port}/swagger` in your browser to explore the API.

Note: swap {port} to your local port — check your terminal output for the exact URL after running `dotnet run`.
