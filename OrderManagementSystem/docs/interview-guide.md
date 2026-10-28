# .NET Senior Backend & Microservices Interview Guide

This guide bridges theoretical .NET interview questions with the real, concrete implementations in this **Order Management System** codebase.

---

## 1. Object-Oriented Programming (OOP) & SOLID Principles

### Q1: How are the SOLID principles demonstrated in this project?

- **S — Single Responsibility Principle (SRP)**:
  - `AuthController` handles only HTTP request routing and status codes; it delegates business rules, user queries, and token generation to `IAuthService`.
  - `JwtTokenGenerator` handles only cryptographic JWT construction. It doesn't query the database or handle password verification.
  - `InventoryHttpClient` encapsulates all outbound HTTP serialization, timeouts, and error handling for `InventoryService`.

- **O — Open/Closed Principle (OCP)**:
  - New discount rules or payment handlers can be introduced by implementing new service interfaces without modifying existing `OrderService` core logic.
  - Exception handling in `GlobalExceptionMiddleware` uses pattern matching on exception types, allowing new custom exceptions to be supported without rewriting the core pipeline.

- **L — Liskov Substitution Principle (LSP)**:
  - `NotFoundException` and `ValidationException` inherit from `AppException`. Any handler accepting `AppException` can process derived exceptions without unexpected behavior.

- **I — Interface Segregation Principle (ISP)**:
  - Rather than having a monolithic `IInventoryService` handle both internal product lookups and administrative mutations, `IInventoryClient` in `OrderService` exposes only `GetProductAvailabilityAsync` and `DeductStockAsync`, which are the exact operations needed by the order domain.

- **D — Dependency Inversion Principle (DIP)**:
  - High-level classes (`OrdersController`, `OrderService`) depend on abstractions (`IOrderService`, `IInventoryClient`, `OrderDbContext`), not concrete implementations.
  - This allows swapping `InventoryHttpClient` for a mock `Mock<IInventoryClient>` in unit tests without touching production code.

---

## 2. ASP.NET Core & Dependency Injection (DI)

### Q2: What are the three DI service lifetimes in ASP.NET Core, and which did we use?

1. **Transient (`AddTransient`)**:
   - A new instance is created every time it is requested from the service provider.
   - Ideal for lightweight, stateless services.

2. **Scoped (`AddScoped`)**:
   - A single instance is created per HTTP request lifecycle.
   - **Used in this project**:
     - `DbContext` (`IdentityDbContext`, `InventoryDbContext`, `OrderDbContext`) is registered as **Scoped**. Why? Because a database context tracks entity state for the duration of a single HTTP request and must be disposed when the request ends.
     - `IAuthService`, `IProductService`, `IOrderService` are registered as **Scoped** to ensure state consistency across the duration of an HTTP request.

3. **Singleton (`AddSingleton`)**:
   - A single instance is created once and shared throughout the lifetime of the application.
   - Used for caching, background service coordinators, or thread-safe state containers.

### Q3: How does the ASP.NET Core Middleware Pipeline work?

- Middleware components form a bi-directional Russian-doll pipeline. Requests pass through middlewares in registration order; responses travel back through them in reverse order.
- In our services, the pipeline is configured as:
  1. `GlobalExceptionMiddleware` (outermost: wraps the entire request execution in a `try/catch` block to guarantee no raw 500 stack traces leak to the client).
  2. `UseSwagger()` & `UseSwaggerUI()` (serves OpenAPI docs).
  3. `UseCors()` (handles cross-origin pre-flight requests).
  4. `UseAuthentication()` (identifies **who** the user is by inspecting the `Authorization: Bearer <token>` header).
  5. `UseAuthorization()` (determines **what** the user is permitted to do based on roles/policies).
  6. `MapControllers()` (terminal endpoint routing).

---

## 3. Security, Authentication & Authorization

### Q4: Explain the difference between Authentication and Authorization.

- **Authentication (AuthN)**: "Who are you?" In our system, this happens when a user submits credentials to `POST /api/auth/login`. The server verifies the password hash and returns a signed JWT.
- **Authorization (AuthZ)**: "What are you allowed to do?" In our system, this is evaluated on incoming requests using `[Authorize(Roles = "Admin")]` or `[Authorize(Roles = "Customer")]`.

### Q5: Why is stateless JWT validation significant in a microservices architecture?

- If every microservice had to call `IdentityService` over the network on every incoming request to validate the token, `IdentityService` would become a critical bottleneck and a single point of failure (temporal coupling).
- Because JWT tokens are signed using HMAC-SHA256 with a shared secret key, `InventoryService` and `OrderService` can validate the signature, expiration, issuer, and audience locally using in-memory CPU operations in microseconds without any network latency.

### Q6: How do we hash passwords securely?

- We use ASP.NET Core's `IPasswordHasher<User>` (`PasswordHasher<User>`).
- It applies **PBKDF2** (Password-Based Key Derivation Function 2) with HMAC-SHA256, cryptographically random salt per user, and multiple thousand iterations.
- This protects against rainbow tables and brute-force attacks.

---

## 4. Entity Framework Core, LINQ & SQLite

### Q7: What is the N+1 Query Problem, and how did we prevent it?

- **The Problem**: When loading an entity with child collections (e.g. `Order` and its `OrderItems`), lazy loading executes 1 query for the parent orders, and then N separate database queries for each order's items, severely degrading performance.
- **The Solution**: We use **Eager Loading** with `.Include(o => o.OrderItems)` in `OrderService.GetOrdersAsync` and `GetOrderByIdAsync`. EF Core generates a single SQL query with a `LEFT JOIN`, returning all required parent and child records in one round-trip.
- We also utilize `.AsNoTracking()` for read queries to bypass entity state tracking overhead in EF Core.

### Q8: Compare SQLite with SQL Server / PostgreSQL in enterprise microservices.

| Feature | SQLite (Used Here) | SQL Server / PostgreSQL |
| :--- | :--- | :--- |
| **Architecture** | In-process, serverless library reading from a single file | Dedicated client-server database engine |
| **Setup Cost** | Zero configuration; runs on any work laptop without IT approvals | Requires installing server daemon, managing services, ports, users |
| **Concurrency** | Single-writer, multi-reader (file-level lock during write transactions) | Row-level locking, high concurrent writes, MVCC |
| **Clustering & Scale** | Difficult to scale horizontally | Native clustering, read replicas, distributed transactions |
| **Ideal Use Case** | Local dev, prototypes, mobile apps, embedded systems | High-throughput enterprise production systems |

---

## 5. Microservices & Distributed System Architecture

### Q9: Why is Database-per-Service mandatory in microservices?

- If multiple microservices shared a single database:
  1. **Schema coupling**: A change by the inventory team could break the order team's queries.
  2. **Performance coupling**: A runaway reporting query on orders could lock product tables.
  3. **Technology lock-in**: Services cannot pick the best database engine (e.g., MongoDB for catalog, PostgreSQL for orders).
- By giving each service its own SQLite database (`identity.db`, `inventory.db`, `orders.db`), each service is truly autonomous and can be modified or redeployed independently.

### Q10: What is the availability vs. consistency trade-off in our Order-to-Inventory workflow?

- **The Trade-off**:
  - `OrderService` synchronously calls `InventoryService` to check stock availability and then sends a deduction request.
  - **Limitation**: This does not constitute a 2-Phase Commit (2PC) or distributed ACID transaction. If concurrent requests arrive simultaneously, stock could be over-allocated.
- **How to explain this in an interview**:
  > *"In this implementation, I opted for synchronous REST communication using `IHttpClientFactory` to keep the architecture understandable, lightweight, and runnable on a work laptop without Kafka or RabbitMQ.*
  > *However, in a production system handling high traffic, I would implement the **Outbox Pattern** with **Kafka** or **RabbitMQ**. When an order is placed, `OrderService` commits the order with `Status = Pending` and an outbox event in a single database transaction. A background worker publishes `OrderCreatedEvent`. `InventoryService` consumes this event, locks and reserves stock atomically, and publishes `InventoryReservedEvent` or `InventoryFailedEvent`. `OrderService` then transitions to `Confirmed` or triggers compensating cancellations (Saga Pattern)."*

---

## 6. Resilient HTTP Communication (`IHttpClientFactory`)

### Q11: Why use `IHttpClientFactory` instead of `new HttpClient()`?

1. **Socket Exhaustion**: Creating and disposing `new HttpClient()` leaves TCP sockets in `TIME_WAIT` state, eventually exhausting available OS ports under load.
2. **DNS Staleness**: Keeping a single static `HttpClient` forever ignores DNS changes.
3. `IHttpClientFactory` pools and manages the lifetime of underlying `HttpMessageHandler` instances, rotating them every 2 minutes while keeping sockets alive.
4. It enables configuring typed clients, base addresses, timeouts, and Polly resilience policies (retries, circuit breakers) in one central place.
