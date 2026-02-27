# Anti-Pattern Violation Mapping

> This project is a **reverse Code Review teaching example** — every rule in `dotnet-guidelines.md` is deliberately violated.

---

## Rule → Violation Design → Code Location

### 1. Security Review

| Rule | Violation Design | Code Location(s) |
|------|-----------------|-------------------|
| **[SEC-1]** SQL Injection | String concatenation/interpolation in SQL queries, `FromSqlRaw` with user input | `Controllers/usercontroller.cs` → `SearchUsers()`, `AdvancedSearch()`;  `Repositories/UserRepository.cs` → `SearchByName()`, `FindByEmail()` |
| **[SEC-2]** XSS | Rendering user-controlled input as raw HTML | `Controllers/usercontroller.cs` → `GetProfileHtml()` |
| **[SEC-3]** Auth & AuthZ | Missing `[Authorize]`, hardcoded credentials, plain text passwords, `[AllowAnonymous]` on login, JWT without expiration | `Controllers/usercontroller.cs` → class-level (no Authorize), `Login()`, `ConnectionString` field;  `Controllers/OrderController.cs` → no Authorize;  `Services/UserService.cs` → `CreateUser()`;  `Repositories/UserRepository.cs` → `AuthenticateUser()` |
| **[SEC-4]** Sensitive Data Exposure | Logging passwords/SSN/tokens, returning passwords in API, sensitive data in query strings, plaintext storage | `Controllers/usercontroller.cs` → `GetAllUsers()`, `Login()`, `ExportUsers()`;  `Services/UserService.cs` → `GetUser()`, `CreateUser()`, `GenerateUserReport()`;  `Models/User.cs` → `UserDto` with password/SSN;  `Middleware/BusinessLogicMiddleware.cs` → logging all headers;  `BackgroundServices/DataSyncService.cs` → logging connection string |
| **[SEC-5]** Insecure Deserialization | `BinaryFormatter` usage, `TypeNameHandling.All` in Newtonsoft | `Controllers/usercontroller.cs` → `ImportData()`;  `Program.cs` → AddNewtonsoftJson config;  `Controllers/OrderController.cs` → `DeserializeOrder()` |
| **[SEC-6]** Path Traversal | User input directly in file paths without validation | `Controllers/usercontroller.cs` → `GetAvatar()` |
| **[SEC-7]** CORS Misconfiguration | `AllowAnyOrigin()` + `AllowAnyMethod()` + `AllowAnyHeader()` | `Program.cs` → CORS configuration |
| **[SEC-8]** Cryptography | MD5/SHA1 for security, ECB mode, hardcoded keys/IVs, custom crypto | `Helpers/CryptoHelper.cs` → all methods;  `Services/UserService.cs` → `EncryptData()`;  `Controllers/usercontroller.cs` → `HashPassword()` |
| **[SEC-9]** Input Validation | No validation on any API input, missing `[FromBody]`/`[FromQuery]`, unbounded collections | `Controllers/usercontroller.cs` → `SearchUsers()`, `Login()`;  `Controllers/OrderController.cs` → `CreateOrderViaGet()`, `BulkCreate()` |
| **[SEC-10]** HTTP Security Headers | No HSTS, no security headers middleware | `Program.cs` → missing security headers |

### 2. Performance Review

| Rule | Violation Design | Code Location(s) |
|------|-----------------|-------------------|
| **[PERF-1]** Async/Await Anti-Patterns | `.Result`, `.GetAwaiter().GetResult()`, `async void`, `Task.Run` wrapping async | `Controllers/usercontroller.cs` → `SearchUsers()`, `GetUser()`, `NotifyUsers()`;  `Services/UserService.cs` → `SendWelcomeEmail()`;  `Services/EmailService.cs` → `SendBulkEmails()`;  `BackgroundServices/DataSyncService.cs` → `StartAsync()`, `ExecuteAsync()` |
| **[PERF-2]** Memory Leaks | No `IDisposable` disposal, static refs to scoped objects, HttpClient not disposed, FileStream not closed | `Controllers/usercontroller.cs` → `NotifyUsers()`, `GetAvatar()`;  `Services/UserService.cs` → `_staticDbContext`, `AuditLog()`, `SendWelcomeEmail()`;  `Services/EmailService.cs` → HttpClient;  `BackgroundServices/DataSyncService.cs` → HttpClient per iteration |
| **[PERF-3]** N+1 Queries | LINQ queries inside loops, missing `.Include()` | `Controllers/usercontroller.cs` → `GetUsersWithOrders()`;  `Services/UserService.cs` → `GenerateUserReport()`;  `BackgroundServices/DataSyncService.cs` → `ExecuteAsync()` |
| **[PERF-4]** Collection Anti-Patterns | Multiple enumerations, `Count() > 0` instead of `Any()`, multiple `Where()`, `List<T>` when `IReadOnlyList` suffices | `Controllers/usercontroller.cs` → `GetStats()`;  `Controllers/OrderController.cs` → `GetOrderReport()`;  `Services/UserService.cs` → `SearchUsers()`, `GetUserIds()` |
| **[PERF-5]** String Operations | Concatenation in loops, `ToLower()` without `StringComparison` | `Controllers/usercontroller.cs` → `GetUsersWithOrders()`, `ExportUsers()`;  `Controllers/OrderController.cs` → `GetSummary()`;  `Services/UserService.cs` → `GenerateUserReport()`;  `Helpers/CryptoHelper.cs` → `HashPassword()`;  `Helpers/StringHelper.cs` → all methods |
| **[PERF-6]** Boxing/Allocations | Value types in `ArrayList` (non-generic collection) | `Controllers/usercontroller.cs` → `GetStats()`;  `Services/UserService.cs` → `GetUserIds()` |
| **[PERF-7]** Database Performance | No `AsNoTracking()`, no pagination, loading full entities | `Controllers/usercontroller.cs` → `GetAllUsers()`, `GetUsersWithOrders()`;  `Controllers/OrderController.cs` → `GetOrderReport()`;  `Repositories/UserRepository.cs` → `GetAllUsers()`;  `Services/UserService.cs` → `GetUser()`, `GenerateUserReport()` |
| **[PERF-8]** Caching | Expensive operations repeated without caching | `Controllers/OrderController.cs` → `GetOrderReport()` |
| **[PERF-MINOR]** DateTime.Now, Regex | `DateTime.Now` everywhere, `Regex` without `Compiled` | `Controllers/OrderController.cs` → `GetOrderReport()`;  `Helpers/StringHelper.cs` → regex fields, `GetTimestamp()` |

### 3. Code Correctness

| Rule | Violation Design | Code Location(s) |
|------|-----------------|-------------------|
| **[CORR-1]** Null References | No null checks, `!` suppression | `Controllers/usercontroller.cs` → `GetUser()`, `SearchUsers()`;  `Services/UserService.cs` → `GetUser()`;  `Helpers/CryptoHelper.cs` → `DecryptBase64()` |
| **[CORR-2]** Exception Handling | Empty catch, `throw ex`, catching generic `Exception` | `Controllers/usercontroller.cs` → `GetUser()`;  `Controllers/OrderController.cs` → `UpdateOrder()`, `DeserializeOrder()`;  `Services/EmailService.cs` → `SendEmail()`;  `BackgroundServices/DataSyncService.cs` → `ExecuteAsync()` |
| **[CORR-3]** Thread Safety | Shared mutable state, non-thread-safe collections, race conditions | `Program.cs` → `GlobalState`;  `Controllers/usercontroller.cs` → `_auditLog`;  `Services/UserService.cs` → `_userCache`, `_operationCount`;  `Middleware/BusinessLogicMiddleware.cs` → `_requestCount`, `_rateLimitTracker` |
| **[CORR-4]** Resource Management | `new HttpClient()` per request, file handles not closed | `Controllers/usercontroller.cs` → `NotifyUsers()`, `GetAvatar()`, `SendDeletionNotification()`;  `Services/UserService.cs` → `SendWelcomeEmail()`, `AuditLog()`;  `Services/EmailService.cs` → `SendEmail()`;  `BackgroundServices/DataSyncService.cs` → `ExecuteAsync()` |
| **[CORR-5]** Equality/Comparison | `GetHashCode()` without `Equals()`, mutable field in hash | `Models/User.cs` → `User.GetHashCode()` |
| **[CORR-6]** Async Correctness | Return null instead of Task.CompletedTask, missing CancellationToken, fire-and-forget | `Controllers/usercontroller.cs` → `DeleteUser()`;  `Services/EmailService.cs` → `ValidateEmail()`, `SendEmail()` |
| **[CORR-7]** DateTime Handling | `DateTime` instead of `DateTimeOffset`, timezone assumptions | `Models/User.cs` → `dtCreatedDate`;  `Models/Order.cs` → `OrderDate`;  `Controllers/OrderController.cs` → `CreateOrderViaGet()`, `GetOrderReport()`;  `Helpers/StringHelper.cs` → `GetTimestamp()`, `IsBusinessHours()`;  `Services/UserService.cs` → `AuditLog()` |

### 4. Architecture & Design

| Rule | Violation Design | Code Location(s) |
|------|-----------------|-------------------|
| **[ARCH-1]** DI Violations | `new` services, Service Locator, captive dependencies, wrong lifetimes | `Program.cs` → DI registrations (Singleton UserService with Scoped DbContext);  `Services/UserService.cs` → `GetDbContext()` via IServiceProvider;  `Repositories/GenericRepository.cs` → IServiceProvider;  `BackgroundServices/DataSyncService.cs` → direct DbContext injection |
| **[ARCH-2]** Layer Violations | Business logic in controllers, data access in presentation | `Controllers/usercontroller.cs` → entire file;  `Controllers/OrderController.cs` → `CreateOrderViaGet()` with business logic |
| **[ARCH-3]** SOLID Violations | God classes, SRP violations, tight coupling | `Controllers/usercontroller.cs` → god controller;  `Services/UserService.cs` → god service (CRUD + email + crypto + audit + reporting) |
| **[ARCH-4]** Design Patterns | Incorrect patterns, missing Strategy pattern | `Controllers/OrderController.cs` → discount logic with if/else instead of Strategy |

### 5. Clean Architecture & DDD

| Rule | Violation Design | Code Location(s) |
|------|-----------------|-------------------|
| **[CLEAN-1]** Domain Layer | Infrastructure deps (EF annotations, Newtonsoft), anemic entities, public setters, no validation, primitive obsession, direct collection exposure, aggregate violations | `Models/User.cs` → all;  `Models/Order.cs` → all |
| **[CLEAN-2]** Application Layer | Direct DbContext usage, domain entities returned to presentation, DTOs with behavior | `Models/User.cs` → `UserDto` class;  `Services/UserService.cs` → entire file |
| **[CLEAN-3]** Infrastructure Layer | Business logic in repositories, no Unit of Work | `Repositories/UserRepository.cs` → `AuthenticateUser()`, `GenerateMonthlyReport()`;  `Repositories/GenericRepository.cs` → `AddOrderWithDiscount()` |
| **[CLEAN-4]** Presentation Layer | Business logic in controllers, direct DbContext | `Controllers/usercontroller.cs` → DbContext injected;  `Controllers/OrderController.cs` → DbContext injected |
| **[CLEAN-5]** CQRS Commands | No CQRS at all, mixed read/write everywhere | Entire project - no command/query separation |
| **[CLEAN-6]** CQRS Queries | No query separation, no pagination | All controller GET methods |
| **[CLEAN-7]** Handler Guidelines | No handlers, no pipeline behaviors | Entire project - no MediatR/handler pattern |

### 6. API Design

| Rule | Violation Design | Code Location(s) |
|------|-----------------|-------------------|
| **[API-1]** REST Violations | GET modifies state, non-idempotent operations | `Controllers/usercontroller.cs` → `GetAllUsers()` modifies audit log;  `Controllers/OrderController.cs` → `CreateOrderViaGet()` |
| **[API-2]** Error Handling | Stack traces in responses, inconsistent formats, generic 500 | `Controllers/usercontroller.cs` → `ImportData()`;  `Controllers/OrderController.cs` → `UpdateOrder()` |
| **[API-3]** Versioning | No API versioning at all | All controllers - no version in routes |
| **[API-4]** Request/Response | No pagination, circular references, exposing internal IDs | `Controllers/usercontroller.cs` → `GetAllUsers()`;  All entity responses |
| **[API-5]** Documentation | No XML docs, no `[ProducesResponseType]` | All controllers |
| **[API-MINOR]** | Inconsistent naming, hardcoded route strings | `Controllers/usercontroller.cs` vs `OrderController.cs` route naming |

### 7. Data Access

| Rule | Violation Design | Code Location(s) |
|------|-----------------|-------------------|
| **[DA-1]** EF Critical | SaveChanges in loops, no transactions, sync calls in async | `Controllers/usercontroller.cs` → `BulkUpdate()`;  `Controllers/OrderController.cs` → `BulkCreate()`;  `Repositories/UserRepository.cs` → `UpdateMultipleUsers()` |
| **[DA-2]** EF Major | Business logic in repos, generic repository, no AsNoTracking, IQueryable leak | `Repositories/GenericRepository.cs` → `AddOrderWithDiscount()`;  `Repositories/UserRepository.cs` → `AuthenticateUser()`, `GenerateMonthlyReport()`;  `Services/UserService.cs` → `GetUsersQueryable()` |
| **[DA-3]** EF Minor | No index configuration | `Program.cs` → `AppDbContext` no `OnModelCreating` |
| **[DA-4]** Dapper-style | SQL injection, connections not disposed | `Repositories/UserRepository.cs` → `SearchByName()`, `FindByEmail()` |
| **[DA-5]** Manual Mapping | Manual mapping instead of AutoMapper | `Repositories/UserRepository.cs` → `MapToDto()` |

### 8. Validation Patterns

| Rule | Violation Design | Code Location(s) |
|------|-----------------|-------------------|
| **[VAL-1]** FluentValidation | No validators registered, no validation at all | Entire project - no FluentValidation |
| **[VAL-2]** Validation Minor | No custom error messages, no shared rules | No validation infrastructure at all |
| **[VAL-3]** DataAnnotations | No `[Required]`, no range/length constraints | `Models/User.cs`, `Models/Order.cs` - no annotations |
| **[VAL-4]** Domain Validation | No invariants in constructors, no Result pattern | `Models/User.cs` → no constructor validation;  `Models/Order.cs` → no constructor validation |

### 9. Configuration Management

| Rule | Violation Design | Code Location(s) |
|------|-----------------|-------------------|
| **[CONFIG-1]** Options Pattern | Direct `IConfiguration` injection, no `IOptions<T>`, mutable options | `Controllers/OrderController.cs` → `_config`;  `Services/UserService.cs` → `_configuration`;  `Models/Order.cs` → `AppConfig` class |
| **[CONFIG-2]** Organization | Config not organized, no defaults | `appsettings.json` → flat structure |
| **[CONFIG-3]** Environment | Hardcoded environment checks in code | `Controllers/OrderController.cs` → `CreateOrderViaGet()` checking env var |
| **Secrets** | Secrets in appsettings.json | `appsettings.json` → connection strings, API keys, passwords |

### 10. Middleware & Pipeline

| Rule | Violation Design | Code Location(s) |
|------|-----------------|-------------------|
| **Middleware Critical** | Wrong pipeline order, auth before exception handling | `Program.cs` → middleware ordering |
| **Middleware Major** | Business logic in middleware, missing `next()`, long operations | `Middleware/BusinessLogicMiddleware.cs` → rate limiting, discount logic, missing next() |
| **Middleware Minor** | No documentation | `Middleware/BusinessLogicMiddleware.cs` |

### 11. Background Services

| Rule | Violation Design | Code Location(s) |
|------|-----------------|-------------------|
| **BG Critical** | Unhandled exceptions, no CancellationToken, scoped in singleton, no graceful shutdown | `BackgroundServices/DataSyncService.cs` → all |
| **BG Major** | Tight loop, no health checks, no retry, blocking StartAsync | `BackgroundServices/DataSyncService.cs` → `ExecuteAsync()`, `StartAsync()` |
| **BG Scoped** | Direct DbContext injection instead of IServiceScopeFactory | `BackgroundServices/DataSyncService.cs` → constructor |

### 12. Resiliency Patterns

| Rule | Violation Design | Code Location(s) |
|------|-----------------|-------------------|
| **RES Critical** | No timeouts, no circuit breaker | `Services/EmailService.cs`, `BackgroundServices/DataSyncService.cs` |
| **[RES-1]** Polly | No retry, no jitter, no resilience policies | `Services/EmailService.cs` → `SendEmail()` |
| **[RES-3]** HTTP Client | No HttpClientFactory, infinite timeout | `Services/EmailService.cs` → `HttpClient.Timeout = 24h` |

### 13. Serialization

| Rule | Violation Design | Code Location(s) |
|------|-----------------|-------------------|
| **SER Critical** | `TypeNameHandling.All`, deserializing untrusted input | `Program.cs` → Newtonsoft config;  `Controllers/OrderController.cs` → `DeserializeOrder()` |
| **[SER-1]** System.Text.Json | Not used at all (using Newtonsoft instead), no source generators | Entire project |
| **[SER-2]** Minor | No `[JsonPropertyName]`, enum as numbers | `Models/Order.cs` → `order_status` enum |
| **[SER-3]** Newtonsoft | Default settings, no ReferenceLoopHandling, no DateTimeZoneHandling | `Program.cs`, `Controllers/OrderController.cs` → `DeserializeOrder()` |

### 14. Messaging Patterns

| Rule | Violation Design | Code Location(s) |
|------|-----------------|-------------------|
| **[MSG-1-4]** | No MediatR, no messaging patterns, no event handling | Entire project - no messaging infrastructure |

### 15. Feature Management

| Rule | Violation Design | Code Location(s) |
|------|-----------------|-------------------|
| **[FM-1]** | Hardcoded feature flags, static checks, flags in controller | `Controllers/OrderController.cs` → `CreateOrderViaGet()` |

### 16. Dependency Management

| Rule | Violation Design | Code Location(s) |
|------|-----------------|-------------------|
| **DEP Critical** | No package lock file | `BadPracticeDemo.csproj` |
| **DEP Major** | Unnecessary Newtonsoft.Json alongside System.Text.Json, direct dep on formatter | `BadPracticeDemo.csproj` |

### 17. Testing

| Rule | Violation Design | Code Location(s) |
|------|-----------------|-------------------|
| **[TEST-1]** | No assertions, tautological tests, shared state, calling production services | `Tests/UserServiceTests.cs` → `Test1()`, `Test2()`, `TestGlobalState()`, `TestExternalService()` |
| **[TEST-2]** | Missing edge cases, overly complex setup, magic values | `Tests/UserServiceTests.cs` → `TestHashPassword()`, `TestComplexScenario()` |
| **[TEST-3]** | Non-descriptive names, duplicate setup, no categories | `Tests/UserServiceTests.cs` → `Test1`, `Test2`, `Test3`, duplicate user creation tests |
| **[TEST-4]** | Hardcoded connection strings, no WebApplicationFactory | `Tests/UserServiceTests.cs` → `TestDatabaseConnection()` |

### 18. Logging & Observability

| Rule | Violation Design | Code Location(s) |
|------|-----------------|-------------------|
| **LOG Critical** | Logging passwords, SSNs, tokens, connection strings | `Controllers/usercontroller.cs` → `GetAllUsers()`, `Login()`;  `Services/UserService.cs` → `GetUser()`, `CreateUser()`;  `BackgroundServices/DataSyncService.cs` → `StartAsync()` |
| **LOG Major** | `Console.WriteLine` instead of `ILogger<T>`, string interpolation in logs, wrong log levels | Entire project - Console.WriteLine everywhere |
| **Health Checks** | No health checks | `Program.cs` - no health check registration |

### 19. Project & Code Structure

| Rule | Violation Design | Code Location(s) |
|------|-----------------|-------------------|
| **[PROJ-1]** | All in one project, no layer separation | Entire solution - single project |
| **[PROJ-2]** | No project boundaries | Single project |
| **[PROJ-3]** | Mixed responsibilities, no DependencyInjection.cs per layer | `Program.cs` → all DI inline |
| **[PROJ-4]** | Namespace `App` doesn't match folder, inconsistent | `BadPracticeDemo.csproj` → RootNamespace=App |
| **[PROJ-5]** | Multiple public types per file | `Program.cs`, `Models/User.cs`, `Models/Order.cs` |
| **[PROJ-6]** | Random member ordering | `Controllers/usercontroller.cs` → static field before instance fields, mixed ordering |
| **[PROJ-7]** | Inconsistent member ordering | Throughout all files |

### 20. Anti-Patterns

| Anti-Pattern | Code Location(s) |
|-------------|-------------------|
| **Sync-over-async** | `Controllers/usercontroller.cs`, `Services/UserService.cs`, `BackgroundServices/DataSyncService.cs` |
| **Service Locator** | `Services/UserService.cs` → `GetDbContext()`, `Repositories/GenericRepository.cs` |
| **God Class** | `Controllers/usercontroller.cs`, `Services/UserService.cs` |
| **Static Abuse** | `Program.cs` → `GlobalState`, `Helpers/CryptoHelper.cs`, `Helpers/StringHelper.cs` |
| **Primitive Obsession** | `Models/User.cs` → Role as string, `Models/Order.cs` → Status as string |
| **Anemic Domain Model** | `Models/User.cs`, `Models/Order.cs` → no behavior |
| **Magic Strings/Numbers** | `Controllers/OrderController.cs` → discount values, `Helpers/StringHelper.cs` → `FormatCurrency()` |
| **Boolean Parameters** | `Controllers/usercontroller.cs` → `ListUsers()` |
| **Deep Nesting** | `Controllers/usercontroller.cs` → `ProcessUser()` |
| **Temporal Coupling** | `Controllers/OrderController.cs` → `Initialize()` must be called first |
| **Train Wreck** | `Controllers/usercontroller.cs` → `UserExtensions.Notes()` |
| **Generic Repository** | `Repositories/GenericRepository.cs` |
| **Leaky Abstraction** | `Services/UserService.cs` → `GetUsersQueryable()` returning IQueryable |
| **Comments explaining what** | `Services/UserService.cs` → constructor comment |
| **Commented-out code** | `Services/UserService.cs` → `OldMethod()` |
| **Hungarian Notation** | `Models/User.cs` → `strUserName`, `boolIsActive`, `dtCreatedDate` |
| **Regions in methods** | `Controllers/OrderController.cs` → `#region` inside method |

### 21. Naming & Style

| Rule | Violation Design | Code Location(s) |
|------|-----------------|-------------------|
| **Non-PascalCase public members** | `Models/User.cs` → `user_ID`, `strUserName`;  `Models/Order.cs` → `order_status` enum |
| **Non-camelCase private fields** | `Controllers/usercontroller.cs` → inconsistent field naming |
| **Abbreviations** | `Models/User.cs` → `SSN` (should be `Ssn`);  `Models/Order.cs` → `OrderID` (should be `OrderId`) |
| **Interface not prefixed with I** | `Models/User.cs` → `UserRepository` interface (no `I` prefix) |
| **Async methods not suffixed** | `Services/EmailService.cs` → `SendEmail`, `SendBulkEmails` |
| **Generic param not prefixed T** | `Repositories/GenericRepository.cs` → `E` instead of `TEntity` |
| **Boolean props without Is/Has/Can** | `Models/User.cs` → `boolIsActive` (has prefix but wrong style) |
| **File name not matching type** | `Controllers/usercontroller.cs` → lowercase file name |

### 22. Modern .NET 8+ Features

| Rule | Violation Design | Code Location(s) |
|------|-----------------|-------------------|
| **No primary constructors** | All classes use verbose constructor pattern |
| **No collection expressions** | `new List<T>()` everywhere instead of `[]` |
| **No `required` modifier** | `Models/User.cs`, `Models/Order.cs` → no required properties |
| **No `TimeProvider`** | `DateTime.Now` everywhere |
| **No `IExceptionHandler`** | No global exception handling |
| **No source generators** | Using Newtonsoft + reflection |
| **No `FrozenDictionary`** | `Dictionary<string, object>` in `GlobalState` |
| **Nullable disabled** | `BadPracticeDemo.csproj` → `<Nullable>disable</Nullable>` |
