VAPESHOPINVENTORYAPI — AUTH SCOPE v1 (Day 190, Nov 2, 2026)
Status: scoping only. No code, tests or deploy yet. Every decision below keeps its reason so it can be revisited when a concrete problem shows up; none of it is an iron rule.

GOALS (ranked)
1. Showcase another skill in the portfolio (real login + token checking).
2. Publish the API URL safely.
3. Protect real shop data (family not using it yet; waiting for a front-end).

ROLES AND ACCESS
- Anonymous: GET /api/catalog (list and by id) only. New PublicProductResponse: Id, Name, Price, StockQuantity, Category. Name and category filters carry over. Reason: a buyer needs nothing else; Sku, LowStockLevel, IsLowStock and CreatedAt are internal. Separate endpoint so each route has one fixed response shape (no conditional DTO, per the Day 144 rejection).
- Cashier (mini manager, can run the shop alone for days): GET /api/products (full ProductResponse), create/edit products, full sale flow incl. edit and cancel of open sales, settlements, expenses, restock, read everything incl. cash balance and history.
- Owner: everything a Cashier can do, plus capital transactions, delete product, and managing user accounts.
- Immutable-records rule stays: roles decide who can create; no new edit/delete endpoints for settlements, capital transactions or restock. Consequence accepted: a cashier typo in those records cannot be fixed in the API.

DESIGN
- Option B: small User table (Id, Username, PasswordHash, Role) + framework PasswordHasher + JWT bearer validation. Not ASP.NET Core Identity (reason: one table, one migration, every line explainable; safer on the production DB). Confirm the exact NuGet package for PasswordHasher on .NET 10 when building.
- Token: JWT, 60 minutes, no refresh tokens yet (refresh tokens come with a front-end). Validate signature, expiry, issuer and audience. Logout = client discards the token (known limitation: the server cannot cancel a token early).
- Login returns the same error for a wrong username and a wrong password.
- No public register endpoint. First Owner is seeded from environment variables when the Users table is empty; the Owner then creates Cashiers. Signing key also from an environment variable, long and random, never committed. App refuses to start if a required setting is missing.
- Built-in rate limiting on the login endpoint (no lockout without Identity).
- Swagger gets an Authorize button.

TESTS
- Shared login helper in the test factory (seeded Owner and Cashier) so the existing 108 tests keep working; one interdependent commit.
- New tests: no token gives 401, wrong role gives 403, role matrix per endpoint group, catalog open to anonymous, login failure cases.
- Postman collection gets a login request and a token variable.

BUILD ORDER
1. User entity + migration (check ProductResponse exposure again here)
2. Hasher + first-Owner seeding
3. Login endpoint + JWT
4. Protect endpoints by role + public catalog
5. Test infrastructure + role tests
6. Rate limiting
7. Swagger Authorize button
8. Postman update
9. HTTPS, deploy, publish the URL (no login deploy before HTTPS; HTTPS needs a domain, a free certificate and a proxy, and the rate limiter needs forwarded-address handling behind that proxy)
One step per afternoon block. Build only after the day's application target is met, including its Tier 2 share; if a day's target is missed, the build waits.

KNOWN LIMITATIONS (accepted)
- No branch concept (branch scale is a later idea).
- No record of who made a record; a cancelled sale leaves no trace.
- No refresh tokens, no token revocation, no lockout.

REVISIT NOTES
- Re-check this scope after about 3 build sessions and change anything that proves wrong.
- Open questions: exact stock number vs in-stock/out-of-stock on the catalog; Cashier rights on expenses and restock (permanent mistakes); whether Identity would have been the better choice.
- This reopens Step 2 deliberately, for auth only.