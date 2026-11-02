# Design Decisions

This document records the significant decisions behind the platform: what was chosen, why, and what it costs. It is written to match the code as it stands; sections are added as features ship.

- [Foundation](#foundation)
- [Authentication](#authentication)
- [Accounts and the ledger](#accounts-and-the-ledger)
- [Known limitations](#known-limitations)

## Foundation

### Two independent applications: an API and an MVC client

**Decision.** The ASP.NET Core Web API and the ASP.NET Core MVC client are separate solutions that communicate only over HTTP. The client keeps its own copies of the API's request and response shapes instead of sharing a contracts library.

**Why.** It was the direct architecture throughout university assignments for full-stack projects. It mirrors how separately deployed services work distributing the work load between a front-end (Client) and a back-end (API).

**Trade-off.** Each Change requires a mirrored change in the client along side the API. However, at the current scope / size of the project it's preferable to coupling a shared project. 

### One command starts everything

**Decision.** `docker compose up` starts SQL Server, the API, and the client, ordered by health checks: the API waits until SQL Server answers queries, and the client waits until the API reports ready.

**Why.** A reviewer should be able to run the whole system without installing anything except Docker. The purpose of this project is to demonstrate programing capability, and unnecessary hurdles towards quick review only hurts that goal.

**Trade-off.** Instead of choosing to have machine specific secerts the project's development secrets (the SA password and the token signing key) are published in `docker-compose.yml`. They're safe only because every port is bound to `127.0.0.1` and both can be overridden with environment variables. A commerical product would not handle secrets in this way, but for the sake of easy review I've opted for this approach.

### Tests run against real SQL Server

**Decision.** API integration tests use Testcontainers to start a disposable SQL Server container, and host the API in memory against it.

**Why.** Ensuring that the constraints, triggers, transactions, and locking are being handled correctly is among the most important tasks for this project. Choosing this approach allows for the same tests to pass when run identically on a laptop and/or in CI.

**Trade-off.** Tests need Docker and take seconds to start rather than milliseconds. Client tests, which have no database, stay as fast unit tests.

### One error format

**Decision.** Every API failure is an RFC 7807 `application/problem+json` response. Unhandled exceptions are logged and returned as a generic 500 with a trace ID; internal details never reach the caller.

**Why.** Improving the ability to quickly see and handle failures is important to me. Now clients handle one error shape everywhere, and the trace ID links any failed request to its log entry without exposing stack traces.

### Code organized by responsibility, not by layer type

**Decision.** The API has five top-level folders, each with one job: `Domain` (what banking is and the rules for changing it), `Features` (how it's exposed over HTTP, one folder per feature), `Identity` (who users are and how they authenticate), `Data` (how things are stored), and `Infrastructure` (framework plumbing with no banking meaning).

**Why.** A reviewer can find anything by asking what it's responsible for. Everything about accounts over HTTP is in `Features/Accounts`; every rule about how money moves is in `Domain/Ledger`. Shared concepts like `Account` live in `Domain` because several features use them.

**Trade-off.** One type per file means many small files: the accounts feature alone has ten. That's the cost of each file having a single, obvious purpose.

## Authentication

### ASP.NET Core Identity instead of hand-rolled authentication

**Decision.** Users, password hashing + salting, lockout, and security stamps come from ASP.NET Core Identity, with `Guid` keys and only the services the API needs (`AddIdentityCore`).

**Why.** Re-inventing the wheel on password security and potentially getting something wrong would not only have no business value when written by hand but also detract from what the goal of this portfolio project is. A polished project with working identity implementations that are maintained and well understood, in my opinion, is more impressive.

### Short-lived JWTs with no refresh tokens

**Decision.** Login issues a 30-minute HS256 access token. There are no refresh tokens; when it expires, the user signs in again. Validation pins the algorithm to HS256 and allows no clock skew.

**Why.** A short lifetime limits how long a stolen token is useful, and leaving out refresh tokens removes a whole class of storage, rotation, and theft-detection problems. Pinning the algorithm blocks tokens that declare a different one, such as `none`.

**Trade-off.** Users are signed out after 30 minutes even while active. While not ideal for professional software, with the understanding this is a open project for review I feel it's acceptable behavior. 

### Logout revokes tokens on the server

**Decision.** Every access token carries the user's security stamp. On every request, the API loads the user and rejects the token if the user no longer exists, is locked out, or has a different stamp. Logout rotates the stamp.

**Why.** A plain JWT stays valid until it expires, so "logout" in most JWT setups only deletes the token in the browser, and a copied token keeps working. Since this is a "mock banking application" I wanted to ensure that logging out anywhere invalidates every outstanding token immediately. 

**Trade-off.** One user lookup per authenticated request, which gives up the "stateless" benefit JWTs are usually chosen for. And because the stamp is per user, logging out on one device signs out every device. That being said I do recognize that per-device sessions would need a session table.

### The browser never sees the token

**Decision.** The MVC client stores the API token inside its own session cookie, which is encrypted by ASP.NET Core Data Protection and marked `HttpOnly` and `SameSite=Lax`. A message handler attaches the token to API calls server-side. The cookie expires exactly when the token does, with no sliding renewal.

**Why.** I needed to find a way to enforce token theft protection. A script running in the page, including injected script, can't read an `HttpOnly` cookie, so it can't steal the token. A session can't outlive the credential it's built on.

### Revoked sessions end cleanly in the client

**Decision.** When the API refuses a session's token, the client's API wrapper throws a dedicated exception, and one global filter signs the cookie out and redirects to the login page with a "session ended" notice. Only a GET request is remembered as the page to return to.

**Why.** I wanted "Sign out everywhere" to look intentional, not like a crash. Handling it once, globally, means every page gets the behavior for free. 

### Deny by default

**Decision.** Every API endpoint requires an authenticated user unless it explicitly opts out with `[AllowAnonymous]`. Only registration, login, the readiness check, and the API reference opt out.

**Why.** An endpoint someone forgets to protect is protected anyway. 

### Lockout and the information it reveals

**Decision.** Five failed sign-ins lock an account for 15 minutes, including for the correct password. An unknown email and a wrong password get the same message.

**Why.** Lockout stops password guessing against any one account. The shared message means login alone can't be used to test which emails are registered.

### Demo accounts with public passwords

**Decision.** Three demo customers are seeded with a password published in the README and on the login page.

**Why.** Since this is a demonstration project, I wanted to pre-seed user's so that a reviewer should be able to sign in within a minute of starting the stack. The demo users are created through the same registration code as real users, so they're indistinguishable from registered accounts.

## Accounts and the ledger

### Money moves through one service, as one conditional update

**Decision.** `LedgerService` is the only code that changes a balance. Each deposit or withdrawal is a single SQL `UPDATE` whose `WHERE` clause carries every rule: the account belongs to the caller, it's active, and (for withdrawals) the balance covers the amount. The update returns the new balance with `OUTPUT`, and the ledger entry records that exact value.

**Why.** I had to learn the concept of concurrency through trial and error. Having two instances running of an account page and attempting to withdrawl from the same account across both tabs should only let one withdrawl occur. SQL Server takes the row lock before evaluating an `UPDATE`'s condition, so concurrent operations on one account are applied one at a time against the latest committed balance, with no retry loops, version columns, or stricter isolation level. `ParallelWithdrawalsNeverOverdraw` fires 20 simultaneous $10 withdrawals at $100 and asserts that exactly 10 succeed and the balance ends at exactly $0.00.

### Account status changes are domain methods; balance changes are not

**Decision.** `Account` exposes `Open`, `Freeze`, `Unfreeze`, and `Close`, each enforcing its rule. There is no method that changes the balance.

**Why.** Status changes are administrative, so loading an account, changing it, and saving should be safe. Money movement may not be. Making that split between the two in the type means nothing can give an in-memory account money.

### The database enforces the rules too

**Decision.** Check constraints reject negative balances, non-positive ledger amounts, and closed accounts holding money. An `INSTEAD OF UPDATE, DELETE` trigger makes ledger entries append-only, raising error 51000. All foreign keys are `Restrict`: users and accounts are never deleted.

**Why.** Using hard database rules helps ensure transations are safer. In theory, they should uphold the data even if the application is bypassed by a bug or hand-written query. 

### Ledger entries record direction and the resulting balance

**Decision.** Each entry stores a positive `Amount` with a `Type` giving its direction, plus `BalanceAfter`: the account's balance immediately after the entry.

**Why.** Choosing to do it this way should enforce proper calculations more safely than if ammounts carried their signs

### An invariant checker verifies the ledger as a whole

**Decision.** For each account checked, every entry's `BalanceAfter` must equal the running total of its history, and the account's balance must equal the total of all its entries. Tests run it after every money-movement scenario; the API runs it over the demo data at startup and refuses to start if it fails.

**Why.** The checker catches the ledger and the balances disagreeing as a whole, which is how a directly edited balance is detected.

### Idempotency is part of every money operation

**Decision.** Deposits and withdrawals require an `Idempotency-Key` header. The ledger stores a record keyed by user and key, with a SHA-256 hash of the request, in the same transaction as the money movement. A retry with the same key and request returns the original entry (`200` instead of `201`); the same key with a different request is refused. Two identical requests racing each other collide on the record's primary key, and the loser rolls back its own balance change and returns the winner's result.


### Another customer's account is "not found"

**Decision.** Requesting an account you don't own returns `404`, exactly as for an account that doesn't exist, from every endpoint: details, history, and money movement. The client renders the same "page not found" for both.

**Why.** A `403 Forbidden` would confirm the account exists. Which wil just encourage malicious actors to continue to probe for information.

### Statements are history over a date range

**Decision.** There's no separate statements feature. The history endpoint takes an optional UTC date range (default: the last 30 days, at most one year) and returns the opening balance, the entries, and the closing balance.

### Account numbers are random; demo numbers are fixed

**Decision.** New accounts get random 10-digit numbers from a cryptographic random source; a unique index guarantees no duplicates. Demo accounts use fixed, published numbers.

**Why.** Random numbers reveal nothing about how many accounts exist and can't be guessed from their neighbors. Demo numbers are fixed so the README can publish them as transfer destinations.

### Demo data goes through the real code paths

**Decision.** The seeder creates customers through `CustomerRegistrar`, accounts through `Account.Open`, history through `LedgerService`, and Cara's frozen and closed accounts through `Freeze()` and `Close()`. History is dated relative to the day the stack starts, recorded oldest first by giving the ledger service a clock set to each transaction's date. Each transaction has a stable idempotency key, so restarts replay history instead of repeating it, and a partly failed seed finishes on the next start.

**Why.** This project is meant to demonstrate the ability and functionality of a proper banking and transaction management application. Having demo data that was seeded outside of the natural cycle wouldn't help illustrate the capabilities of the program. 

## Known limitations
These are deliberate scope decisions for a portfolio project, with what a production system would need instead.
- **No multi-factor authentication.** Typically you would expect / want added security measures especially for sensitive data; However, the added layers of restriction to beign able to review the core functionality of the program seemed over the top for the scope of this project. That being said, this would be the first feature added should this project be taken to commerical production standards.
- **No email confirmation or password reset.** Similarly to multi-factor authentication, additional hurdles to quick testing were defered. In a commerical production email verification would be a sub-step to account creation.
- **No rate limiting.** Lockout protects individual accounts, but nothing throttles requests per client. ASP.NET Core's built-in rate limiter would cover login and registration, and in a commercial production this would be heavily implemented as security measures.
- **Data Protection keys aren't persisted.** The client's cookie keys live inside its container, so recreating the container signs everyone out. Production would persist the keys to shared storage and encrypt them at rest.
- **HTTP only in the demo.** The Docker demo runs over plain HTTP on localhost. The session cookie is marked `Secure` automatically when served over HTTPS, and production would add HTTPS redirection and HSTS.
- **Dates are UTC.** Statement ranges and transaction dates use whole UTC days. A production bank would store each customer's time zone and present dates in it.
- **No paging within a statement.** A statement covers at most one year, which bounds each response. Production would page large statements.
- **Idempotency records are kept forever.** Normally they should expire 24 hours to a few days.