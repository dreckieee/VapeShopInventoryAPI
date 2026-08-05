# Design Decisions & Development Log

## Design decision: stock deduction timing
Stock is deducted from `Product.StockQuantity` at `CloseSale` time, not at `AddSaleItem` time. Built for a single-register, in-person retail context — not designed for multiple simultaneous registers against the same stock pool. Two registers could both add the last unit of a low-stock item to separate open sales before either closes; this is an accepted, documented tradeoff, not an oversight.

## Design decision: `CancelSale` scope and limitation
`CancelSale` permanently deletes an open sale and any attached `SaleItem` rows, uniformly regardless of item count, since nothing affects stock until `CloseSale` runs. No audit trail is kept for cancelled sales — acceptable since an unclosed sale was never a completed transaction. Once a sale is closed, it cannot be deleted or cancelled through the API by design.

## Design decision: FK relationships use `Restrict`, not `Cascade`
`SaleItem → Sale`, `SaleItem → Product`, `DeliveryItem → Expense`, and `DeliveryItem → Product` foreign keys all use `OnDelete(DeleteBehavior.Restrict)` rather than `Cascade`, deliberately. `Cascade` would silently delete child rows whenever a parent is removed — convenient, but it hides deletion behavior inside EF Core's internals rather than making it visible in application code. For a POS system where sale/inventory/delivery audit trails matter, an explicit, visible removal path is safer than an implicit one, even at the cost of extra code.

## Design decision: DTO construction pattern
Response DTOs (`ProductResponse`, `SaleResponse`, `SaleItemResponse`, `ExpenseResponse`, `DeliveryItemResponse`, `RestockResponse`) are `record` types with `init`-only properties, plus a static factory method (`FromX`) for mapping logic. An earlier private-constructor + `[JsonConstructor]` pattern was reconsidered as over-engineered for flat DTOs with no real invariant to protect — needing `[JsonConstructor]` at all was itself a signal the pattern fought `System.Text.Json`'s natural model. Request DTOs (`CreateProductRequest`, `RestockRequest`, `RestockItemRequest`) keep public setters unchanged, since framework model-binding constructs them externally and they have no invariant to protect.

## Refactor: explicit namespace declarations across Api project (Day 98–99)
All files in the main API project and test project now declare explicit namespaces
(`VapeShopInventoryAPI.Api` for root files, `VapeShopInventoryAPI.Api.DTOs` for DTOs/,
`VapeShopInventoryAPI.Api.Exceptions` for Exceptions/), replacing the previous
implicit/global namespace. `Migrations/` files were already namespaced by EF Core
codegen and needed no change. `Program.cs` stays in the global namespace by structural
necessity (top-level statements can't carry a namespace declaration) and instead takes
a `using VapeShopInventoryAPI.Api;` directive for the types it references.

Migrated across two sessions: paused mid-way Day 98 in a known-broken intermediate
state (expected — partial migration surfaces cascading CS0246 errors in any
not-yet-migrated file referencing an already-migrated type), resumed and completed
Day 99. Also required updates to the test project (`CustomWebApplicationFactory.cs`,
`ProductsApiTests.cs`, `SalesApiTests.cs`) to add `using` directives for the newly
namespaced types. Full build and all 11 NUnit tests verified passing before commit.

Committed as a single bulk commit per the standing one-file-per-commit exception for
uniform, zero-behavior-change mechanical refactors (see KEY_DECISIONS.md, General
working rules).

## Known issue (fixed): orphaned `SaleItem` rows on quantity-reduce-to-zero
`Sale.ReduceSaleItemQuantity` removes a `SaleItem` from its in-memory collection once its quantity reaches zero. Because the `SaleItem → Sale` foreign key is `Restrict` and required, EF Core had no valid action for an entity severed from its parent's tracked collection without explicit instruction — it threw `DbUpdateException` on save. Fixed by explicitly calling `_context.SaleItems.Remove(saleItem)` before saving whenever the domain call removes an item. Covered by a regression test.

## Known issue (fixed): `DeleteProduct` had no guard against existing sale records
Previously attempted deletion unconditionally, resulting in a bare `500` whenever a product had any existing `SaleItem` reference. Fixed by adding an explicit existence check before deletion, returning `409 Conflict` with a clear message, plus a fallback catch for unexpected constraint failures.

## Known issue (open): `ExpensesController`'s delete endpoint has no guard against `DeliveryItem` references (Day 102)
Deleting an `Expense` that still has `DeliveryItem` rows pointing at it (e.g. a Restock-category expense) will hit the database's `Restrict` constraint and surface a raw `DbUpdateException` — likely an unhandled `500` — instead of a clean `409 Conflict`. `DeleteProduct` already guards against this exact failure mode for `SaleItem` references (see above); `ExpensesController` needs the same fix applied for `DeliveryItem`. Discovered while building the restock feature, not yet fixed.

## Known issue (fixed): environment-specific DB path mismatch (Day 94)
After deployment, `GET /api/Products` returned a `500` with `SQLite Error 1: 'no such table: Products'`. Root cause: the connection string used a relative path (`Data Source=vapeshop.db`), and EF Core migrations were run from the repo folder while the systemd service's working directory was different — two different folders, two different (and initially empty) database files. Fixed immediately by copying the migrated `.db` file to the service's working directory, then permanently by adding `appsettings.Production.json` with an absolute path override for the connection string, so this can't recur regardless of which folder migrations are run from.

## Testing approach
Tests use `WebApplicationFactory<Program>` via a custom `CustomWebApplicationFactory`, booting the API in-process and swapping the real SQLite database for an isolated in-memory SQLite connection. Requests go through `HttpClient`, not a real network call. `Program.cs` declares `public partial class Program { }` at file scope so the test project can reference the app's entry point.

Sales tests clean up via `PUT /api/Sales/{id}/cancel` (no plain delete endpoint exists for sales, by design — see above). Closed sales can't be cancelled by design; teardown detects this and logs an explicit skip rather than failing.

Beyond automated tests, the full Product → Sale → SaleItem → CloseSale flow was manually verified against the live deployed instance (Day 94) — confirming stock deduction on close and the closed-sale rejection guard, validating the deployed environment itself, not just local/in-memory tests.

The restock endpoint (Day 102) was manually verified end-to-end via Swagger against the local dev database — single-product restock, multi-product batch, and the duplicate-ProductId-different-unit-cost scenario — but has no automated NUnit coverage yet; queued for next session.

## Deployment history

**Day 89 — Two providers blocked on capacity.** Oracle Cloud's `VM.Standard.A1.Flex` failed with out-of-capacity errors in Singapore West — a known Always Free tier issue, not a config problem. Azure's `Standard_B1s` was confirmed unavailable in Southeast Asia entirely. Pivoted to DigitalOcean (paid, no free-tier capacity lottery) over Hetzner, due to Hetzner's biometric ID verification requirement.

**Day 90 — ProductResponse construction locked down (later revised Day 91).** Deployment held — card temporarily unavailable.

**Day 91 — DTO pattern revision; test infrastructure overhaul.** Adopted `WebApplicationFactory<Program>` for in-process integration testing, replacing `IAPIRequestContext` (which requires a real HTTP server and can't talk to an in-process `TestServer`). Surfaced and fixed a test-isolation gap where tests were silently running against real dev data.

**Day 92 — Two production bugs found via test coverage.** Writing reduce-to-zero and close-sale tests surfaced the orphaned-SaleItem and DeleteProduct FK-guard bugs (see Known Issues above). Deployment deferred again in favor of fixing these first.

**Day 93 — Test suite design cleanup.** Found and fixed a self-referential assertion in `AddSaleItem_ValidRequest_ReturnsOk` that had been silently passing without actually testing anything. Azure subscription cancellation confirmed complete.

**Day 94 — Deployment complete.** Provisioned a DigitalOcean droplet (1 vCPU / 2GB RAM / 50GB SSD, Ubuntu 24.04 LTS, Singapore, $12/mo) with SSH key auth. Installed .NET 10 SDK, cloned the repo, published via `dotnet publish`, and configured a systemd service with `Restart=always`. Configured `ufw` to allow SSH and the app port before enabling the firewall. Removed the `IsDevelopment()` guard around Swagger for demo accessibility. Found and fixed the DB path mismatch bug (see Known Issues above). Manually verified the full CRUD + Sale lifecycle against the live instance.

**Day 99 — Namespace migration completed.** Resumed Day 98's paused mid-migration
state; resolved cascading CS0246 errors file-by-file (root entities → DTOs →
controllers → test project), verified clean build and full test suite pass, single
bulk commit.

**Day 101 — Day 100 features deployed to production.** Migrations `AddLowStockLevelToProduct` and `AddCreatedAtToExpense` applied cleanly against the production DB, including the `CreatedAt` backfill (`UPDATE Expenses SET CreatedAt = Date`). Published and restarted via systemd; verified live through Swagger (name search, `IsLowStock` flag, Sales/Expenses monthly filters, `IncomeController`).

**Day 102 — Restock built and locally verified, not yet deployed.** See below for full design writeup. Category filter (Day 101) also remains undeployed as of Day 102.

## Design decision: two separate deploys over one batched deploy (Day 101)
Day 100's features were finished and tested; the restock endpoint and `ProductHistory` feature (see below) were not yet started. Deployed Day 100 on its own rather than holding it behind unstarted work — bundling tested code behind untested code blocks it for no reason, and smaller, more frequent deploys are the target habit, not the exception.

## Day 102 — Restock confirmation, feature queue scoping, and full build

### Restock category handling
Confirmed `Expense`'s existing shape already supports restock with no schema change — it already had a required, non-empty `Category` string. Settled the drift-prevention approach: added `Expense.RestockCategory` const ("Restock"), auto-assigned on every restock-generated expense, never user-supplied. This closes the category-drift risk for the restock path specifically. The broader problem — `Expense.Category` is free-text everywhere else, so manually-entered categories can drift ("Rent" vs "rent") and silently break filtering — remains open, and is now a queued future item: replacing the free-text field with a proper `Category` entity + FK, with user-managed CRUD rather than a fixed enum (chosen deliberately for portfolio signal: full relational CRUD + delete-guard pattern, not just the simpler option).

### New feature set scoped (not built)
Surfaced and scoped a much larger set of features prompted by two real operational needs: (1) real-time cash-on-hand tracking split across payment methods, since income reporting alone can't answer "what's physically in the drawer right now" — withdrawals can happen before end-of-day audit; (2) handling bad products/returns/replacements across three distinct scenarios (supplier defect pre-sale, customer refund, replacement — potentially with a different product). Both features share a hard dependency: a `PaymentMethod` field on `Sale` and `Expense`, which doesn't exist yet. Full breakdown, sequencing, and dependency chain tracked in the project's ACTIVE_PHASE working notes.

Also confirmed: receivable/payable tracking stays informal for now — no `Customer`/`Supplier` entity. A free-text `Party` field on the future settlement ledger carries per-person/per-supplier detail without committing to a real entity, deliberately deferred until per-person tracking is an actual confirmed need rather than a hypothetical one. Designed FK-migration-ready in case that need changes later.

### Restock's final design
Restock moved from an initially simple "one Expense, no itemization" design to a fully itemized batch endpoint, after working through what "reviewing deliveries" actually requires:
- `POST /api/restock`, batch shape (`Items: [...]`) — chosen over nesting under `/products/{id}/restock`, since a delivery event isn't an action on one existing Product resource. It's closer to "create a delivery event" that happens to cover one or more products. A single-product restock is just a batch of one; no separate single-item endpoint was needed.
- One `Expense` per call (one delivery invoice = one Expense), not one Expense per product line — even when the batch covers multiple products, or the same product delivered at two different unit costs.
- Per-product cost tracking was judged genuinely important for reviewing past deliveries, not just a nice-to-have. Considered and rejected a cheaper alternative first — an auto-generated, itemized `Expense.Description` string listing what was delivered. Rejected because a formatted string can't be queried or aggregated ("how much did I spend on Product X across all deliveries this year" requires parsing free text, which SQL/LINQ can't do), and it broke from an established principle in this codebase: don't store derived/structured information as an opaque blob when it can be modeled as real relational data.
- This led to a new `DeliveryItem` entity: `ProductId`, `Quantity`, `UnitCost`, a computed `TotalCost` (`Quantity * UnitCost`, never stored), FK to both `Expense` and `Product` (both `Restrict`, consistent with the existing `SaleItem` FK pattern — see above).
- `DeliveryItem` deliberately has no navigation properties (FK-only ints) and no `Edit` method. No nav properties because nothing inside `DeliveryItem` needs to call behavior on `Product`/`Expense` — that orchestration lives in the controller, not the entity. No `Edit` because a correction isn't a simple field update: `Product.StockQuantity` and `Expense.Amount` are already derived from the original values at restock time, so editing a `DeliveryItem` afterward would silently desync both without a dedicated reversal/adjustment flow, which doesn't exist yet.
- `RestockRequest.TotalAmount` was dropped entirely once itemized cost landed — `Expense.Amount` is now always computed server-side as the sum of `Quantity * UnitCost` across all line items, never caller-supplied. Same "computed, not independently editable" principle already applied to `Product.IsLowStock`.

### Build and verification
Built `DeliveryItem` entity, DbContext registration with both FK relationships, migration (`AddDeliveryItem`, applied locally), `RestockController` (validates every `ProductId` exists before creating anything — all-or-nothing, so a partial failure mid-batch can never leave a persisted `Expense` that doesn't match what was actually restocked), and all four DTOs (`RestockRequest`, `RestockItemRequest`, `RestockResponse`, `DeliveryItemResponse`).

Manually verified end-to-end via Swagger: single-product restock, multi-product batch, and the duplicate-ProductId-different-unit-cost scenario (same product delivered twice at different costs in one call) — confirmed both delivery lines itemized correctly, the Expense's total correctly summed both, and the product's stock updated once with the combined quantity.

### Tooling
`dotnet-ef` updated from 10.0.9 to 10.0.10 via `dotnet tool update --global dotnet-ef`, matching the runtime version and resolving the version-mismatch warning that had been a standing cosmetic item since Day 100/101.

## Resolved: restock endpoint + ProductHistory scoping (originally Day 101, restock completed Day 102)
Restock endpoint (see Day 102 entry above for full design) is now built and locally verified — the open questions from Day 101 (category handling, route shape, single vs multi-product scope) are all settled. `ProductHistory` remains scoped but not built — see ACTIVE_PHASE working notes for current build queue and dependency order. Counter retirement (`ReductionFrequency`/`TotalQuantityReduction` → computed from history) remains queued behind `ProductHistory`.