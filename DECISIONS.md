# Design Decisions & Development Log

## Design decision: stock deduction timing
Stock is deducted from `Product.StockQuantity` at `CloseSale` time, not at `AddSaleItem` time. Built for a single-register, in-person retail context — not designed for multiple simultaneous registers against the same stock pool. Two registers could both add the last unit of a low-stock item to separate open sales before either closes; this is an accepted, documented tradeoff, not an oversight.

## Design decision: `CancelSale` scope and limitation
`CancelSale` permanently deletes an open sale and any attached `SaleItem` rows, uniformly regardless of item count, since nothing affects stock until `CloseSale` runs. No audit trail is kept for cancelled sales — acceptable since an unclosed sale was never a completed transaction. Once a sale is closed, it cannot be deleted or cancelled through the API by design.

## Design decision: FK relationships use `Restrict`, not `Cascade`
Both `SaleItem → Sale` and `SaleItem → Product` foreign keys use `OnDelete(DeleteBehavior.Restrict)` rather than `Cascade`, deliberately. `Cascade` would silently delete child rows whenever a parent is removed — convenient, but it hides deletion behavior inside EF Core's internals rather than making it visible in application code. For a POS system where sale/inventory audit trails matter, an explicit, visible removal path is safer than an implicit one, even at the cost of extra code.

## Design decision: DTO construction pattern
Response DTOs (`ProductResponse`, `SaleResponse`, `SaleItemResponse`) are `record` types with `init`-only properties, plus a static factory method (`FromX`) for mapping logic. An earlier private-constructor + `[JsonConstructor]` pattern was reconsidered as over-engineered for flat DTOs with no real invariant to protect — needing `[JsonConstructor]` at all was itself a signal the pattern fought `System.Text.Json`'s natural model.

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

## Known issue (fixed): environment-specific DB path mismatch (Day 94)
After deployment, `GET /api/Products` returned a `500` with `SQLite Error 1: 'no such table: Products'`. Root cause: the connection string used a relative path (`Data Source=vapeshop.db`), and EF Core migrations were run from the repo folder while the systemd service's working directory was different — two different folders, two different (and initially empty) database files. Fixed immediately by copying the migrated `.db` file to the service's working directory, then permanently by adding `appsettings.Production.json` with an absolute path override for the connection string, so this can't recur regardless of which folder migrations are run from.

## Testing approach
Tests use `WebApplicationFactory<Program>` via a custom `CustomWebApplicationFactory`, booting the API in-process and swapping the real SQLite database for an isolated in-memory SQLite connection. Requests go through `HttpClient`, not a real network call. `Program.cs` declares `public partial class Program { }` at file scope so the test project can reference the app's entry point.

Sales tests clean up via `PUT /api/Sales/{id}/cancel` (no plain delete endpoint exists for sales, by design — see above). Closed sales can't be cancelled by design; teardown detects this and logs an explicit skip rather than failing.

Beyond automated tests, the full Product → Sale → SaleItem → CloseSale flow was manually verified against the live deployed instance (Day 94) — confirming stock deduction on close and the closed-sale rejection guard, validating the deployed environment itself, not just local/in-memory tests.

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