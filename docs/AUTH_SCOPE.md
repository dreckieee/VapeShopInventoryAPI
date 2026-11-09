VAPESHOPINVENTORYAPI — AUTH SCOPE v2 (v1 Day 190, Nov 2, 2026; revised Day 197, Nov 9, 2026)
Status: build steps 1-3 and the test login helper are DONE (Days 191-196). Step 4 (protect endpoints by role + public catalog) is designed and not yet built. Every decision below keeps its reason so it can be revisited when a concrete problem shows up; none of it is an iron rule.

GOALS (ranked)
1. Showcase another skill in the portfolio (real login + token checking).
2. Publish the API URL safely.
3. Protect real shop data (family not using it yet; waiting for a front-end).

ROLES AND ACCESS
- Anonymous: GET /api/catalog (list and by id) only. New PublicProductResponse: Id, Name, Price, IsInStock, Category. Name and category filters carry over. Reason: a buyer needs nothing else; Sku, StockQuantity, LowStockLevel, IsLowStock and CreatedAt are internal. Id stays as the key for the by-id route. Separate endpoint so each route has one fixed response shape (no conditional DTO, per the Day 144 rejection).
- Stock display (decided Day 197): the catalog shows IsInStock (bool, StockQuantity > 0), not the exact number. Reason: it hides inventory depth and total value, it is the safe default if real shop data ever sits behind a public URL (one droplet, one DB), and adding an exact or capped number later is non-breaking while removing one is breaking. Rejected: always exact (leaks depth and value); exact only when low (ties a public display to the LowStockLevel reorder threshold and leaks low-stock status); stock levels (new concept, no buyers). Name follows IsLowStock and IsClosed. Revisit when online orders are scoped (checkout must re-check stock on the server; an exact or capped 'N left' could be added then) or before real shop data is public.
- Cashier (mini manager, can run the shop alone for days): GET /api/products (full ProductResponse), create/edit products, full sale flow incl. edit and cancel of open sales, settlements, expenses, restock, read everything incl. cash balance and history.
- Cashier rights on expenses and restock (decided Day 197): the Cashier keeps both. Reason: the shop has one staff member, the Cashier, who runs everything including deliveries when the Owner is away; Owner-only would contradict 'Cashier can run the shop alone'. Accepted cost: a wrong restock or expense entry cannot be undone through the API (same as the immutable-records rule). Rejected: Owner-only create/edit of expenses and restock; an Owner-only reversal endpoint (reopens the immutable-records rule, not now).
- Owner: everything a Cashier can do, plus capital transactions, delete product, and managing user accounts.
- Immutable-records rule stays: roles decide who can create; no new edit/delete endpoints for settlements, capital transactions or restock. Consequence accepted: a cashier typo in those records cannot be fixed in the API.

DESIGN
- Option B: small User table (Id, Username, PasswordHash, Role) + framework PasswordHasher + JWT bearer validation. Not ASP.NET Core Identity (reason: one table, one migration, every line explainable; safer on the production DB). Hasher package question closed Day 192: PasswordHasher<TUser> ships in Microsoft.Extensions.Identity.Core, no extra package.
- Token: JWT, 60 minutes, no refresh tokens yet (refresh tokens come with a front-end). Validate signature, expiry, issuer and audience. Logout = client discards the token (known limitation: the server cannot cancel a token early).
- Login returns the same error for a wrong username and a wrong password.
- No public register endpoint. First Owner is seeded from environment variables when the Users table is empty. Signing key also from an environment variable, long and random, never committed. App refuses to start if a required setting is missing.
- Built-in rate limiting on the login endpoint (no lockout without Identity).
- Swagger gets an Authorize button.

STEP 4 DESIGN (decided Day 197)
- Role names: a small AuthRoles static class (own file, own commit, built first) with Owner = nameof(UserRole.Owner), Cashier = nameof(UserRole.Cashier) and OwnerOrCashier = Owner + "," + Cashier. Reason: [Authorize(Roles = ...)] takes a string, so the enum cannot be passed directly; one place for the spelling avoids a typo in a combined string, which would lock the Cashier out while every Owner-client test stays green. Still derived from the enum, so no second source of truth. Rejected: authorization policies (a new concept for no gain with two roles).
- Attributes: class-level [Authorize(Roles = ...)] on each controller, tightened per action where needed (a method-level [Authorize] is ANDed with the class-level one, so it can only tighten; Roles = "Owner,Cashier" inside one attribute means OR). [AllowAnonymous] is the only loosener and is used only on the new catalog and on login.
- Order: one controller per commit, starting with CapitalTransactionsController (Owner-only, the cleanest proof that roles work). Each controller's feat: commit is followed at once by its test: commit (anonymous 401, Cashier 403 where the Owner restriction applies, and a test on the exact action for per-action tightening), mutation-checked by removing the attribute. Reason: Swagger and Postman have no token until steps 7 and 8, so the tests are the only proof, and waiting for step 5 would leave a window where a wrong or missing attribute fails nothing.
- Last commit of step 4: a FallbackPolicy requiring an authenticated user, so any controller left unmarked is closed by default. It needs AddAuthorization. It stops anonymous exposure only, not a Cashier reaching an Owner-only door (role tests cover that). Login, /me and the catalog must be marked [AllowAnonymous] (or already allowed) before it lands, otherwise nobody can log in.
- The first role test also proves RoleClaimType = 'role' works for role checks.
- A 403 from the framework has an empty body, the same accepted limitation as the empty 401.

TESTS
- Shared login helper in the test factory (DONE Day 195: Owner and Cashier clients with tokens minted from the real TokenService), helper proven through /api/auth/me (Days 195-196), and every existing test file switched to the Owner client, one file per commit (DONE Day 196, 112 tests). The order keeps the suite green at every commit.
- Per-controller 401/403 tests are written in step 4 (above). Step 5 holds the rest: role matrix per endpoint group, catalog open to anonymous, login failure cases, and the remaining auth unit tests.
- Postman collection gets a login request and a token variable.

BUILD ORDER
1. User entity + migration (DONE Day 191)
2. Hasher + first-Owner seeding (DONE Day 192)
3. Login endpoint + JWT + token validation + /me (DONE Days 193-194)
3.5. Test login helper, its proof, switch of existing tests to the Owner client (DONE Days 195-196)
4. Protect endpoints by role + public catalog (designed Day 197, not built)
5. Role tests and the rest of the test list
6. Rate limiting
7. Swagger Authorize button
8. Postman update
9. HTTPS, deploy, publish the URL (no login deploy before HTTPS; HTTPS needs a domain, a free certificate and a proxy, and the rate limiter needs forwarded-address handling behind that proxy; never deploy auth work piecemeal)
One step per afternoon block. Build only after the day's application target is met, including its Tier 2 share; if a day's target is missed, the build waits (exceptions have been my call, scoped to the day, not rule changes).

KNOWN LIMITATIONS (accepted)
- No branch concept (branch scale is a later idea).
- No record of who made a record; a cancelled sale leaves no trace.
- No refresh tokens, no token revocation, no lockout.
- Unknown-username timing difference on login (mitigation: login rate limiting, step 6).
- Empty-body 401 and 403 from the framework.

REVISIT NOTES
- Re-check this scope after about 3 build sessions and change anything that proves wrong.
- Closed Day 197: exact stock number vs in-stock/out-of-stock (IsInStock); Cashier rights on expenses and restock (Cashier keeps both).
- Still open: no step creates a Cashier (the seeder makes only the Owner; the scope gives the Owner user management but no build step covers it): decide after step 4 between an Owner-only create-user endpoint and a documented manual insert. Whether Identity (or a managed provider) would have been the better choice (view: yes for production, no for this project's goal).
- This reopens Step 2 deliberately, for auth only.