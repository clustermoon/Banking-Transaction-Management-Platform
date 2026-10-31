# Design Decisions

This document records the significant decisions behind the platform: what was chosen, why, and what it costs. It is written to match the code as it stands; sections are added as features ship.

- [Foundation](#foundation)
- [Authentication](#authentication)
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

## Known limitations
These are deliberate scope decisions for a portfolio project, with what a production system would need instead.

- **No multi-factor authentication.** Typically you would expect / want added security measures especially for sensitive data; However, the added layers of restriction to beign able to review the core functionality of the program seemed over the top for the scope of this project. That being said, this would be the first feature added should this project be taken to commerical production standards.
- **No email confirmation or password reset.** Similarly to multi-factor authentication, additional hurdles to quick testing were defered. In a commerical production email verification would be a sub-step to account creation.
- **No rate limiting.** Lockout protects individual accounts, but nothing throttles requests per client. ASP.NET Core's built-in rate limiter would cover login and registration, and in a commercial production this would be heavily implemented as security measures.
- **Data Protection keys aren't persisted.** The client's cookie keys live inside its container, so recreating the container signs everyone out. Production would persist the keys to shared storage and encrypt them at rest.
- **HTTP only in the demo.** The Docker demo runs over plain HTTP on localhost. The session cookie is marked `Secure` automatically when served over HTTPS, and production would add HTTPS redirection and HSTS.