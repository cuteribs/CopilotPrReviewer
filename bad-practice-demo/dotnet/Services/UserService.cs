// ============================================================================
// [ARCH-3] GOD SERVICE - does everything, violates SRP massively
// [ARCH-1] Uses 'new' for services, Service Locator pattern
// [CLEAN-2] Application layer with direct infrastructure
// [CLEAN-3] Business logic in infrastructure-like code
// [CORR-3] Thread safety issues - shared mutable state in singleton
// [PERF-2] Memory leaks, static references to scoped objects
// [LOG] String interpolation in logs, logging sensitive data
// [ANTI-PATTERN: God class, Service Locator, static abuse]
// ============================================================================

using App.Models;
using App.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace App.Services;

// [ARCH-3] God class - handles users, emails, crypto, auditing, reporting
// [ANTI-PATTERN: God class] - hundreds of lines, multiple responsibilities
public class UserService
{
    // [PERF-2] Static reference to scoped object
    private static AppDbContext _staticDbContext;

    // [CORR-3] Shared mutable state in singleton without synchronization
    private Dictionary<int, User> _userCache = new Dictionary<int, User>();
    private int _operationCount = 0;

    // [ARCH-1] Service Locator pattern
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    // [ANTI-PATTERN: Comments explaining what - code should be self-documenting]
    // This constructor sets up the user service with its dependencies
    public UserService(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    // [ARCH-1] Service Locator anti-pattern
    private AppDbContext GetDbContext()
    {
        // [ARCH-1] Service Locator: resolving from IServiceProvider in business logic
        return _serviceProvider.GetService<AppDbContext>();
    }

    // ===== User CRUD - business logic that should be in domain =====

    public User GetUser(int id)
    {
        // [CORR-3] Race condition - non-thread-safe dictionary in singleton
        if (_userCache.ContainsKey(id))
            return _userCache[id];

        var db = GetDbContext(); // [ARCH-1] Service Locator

        // [PERF-7] No AsNoTracking for read
        var user = db.Users
            .Include(u => u.Orders)
            .Include(u => u.Manager)
            .FirstOrDefault(u => u.user_ID == id);

        // [CORR-1] Null dereference - no null check before accessing properties
        // [SEC-4] Logging sensitive data
        Console.WriteLine($"Retrieved user {user.strUserName} with SSN {user.SSN} and password {user.strPassword}");

        // [LOG] String interpolation in log messages (should use structured logging)
        // [LOG] Using Console.WriteLine instead of ILogger<T>

        _userCache[id] = user; // [CORR-3] Thread-unsafe write
        return user;
    }

    public void CreateUser(string name, string email, string password, string ssn)
    {
        // [SEC-3] No password hashing - storing plaintext
        // [VAL-4] No domain validation
        // [CLEAN-1] Should be entity constructor with invariants

        var user = new User
        {
            strUserName = name,
            strEmail = email,
            strPassword = password,  // [SEC-3][SEC-4] Plain text password
            SSN = ssn,              // [SEC-4] Sensitive data stored plain
            boolIsActive = true,
            dtCreatedDate = DateTime.Now, // [CORR-7] DateTime.Now
            Role = "user"
        };

        var db = GetDbContext();
        db.Users.Add(user);
        db.SaveChanges(); // [DA-1] Sync call in what could be async

        // [PERF-2] Static reference to request-scoped dbcontext
        _staticDbContext = db;

        // [CORR-3] Modifying static state
        GlobalState.ActiveUsers.Add(name);

        // [SEC-4] Logging password and SSN
        Console.WriteLine($"Created user {name} with password {password} and SSN {ssn}");

        // [ARCH-3] SRP violation - service also sends emails directly
        SendWelcomeEmail(email, name, password);

        // [ARCH-3] SRP violation - service also does auditing
        AuditLog($"User created: {name}, password: {password}");
    }

    // [ARCH-3] Email responsibility doesn't belong here
    public void SendWelcomeEmail(string email, string name, string password)
    {
        // [CORR-4] New HttpClient per request - socket exhaustion
        var client = new HttpClient();
        // [RES-3] No timeout, no resilience policy

        // [SEC-4] Sending password in email content
        var content = new StringContent(
            $"{{\"to\":\"{email}\",\"body\":\"Welcome {name}! Your password is: {password}\"}}",
            Encoding.UTF8,
            "application/json"
        );

        // [PERF-1] .Result - sync over async in service
        var result = client.PostAsync("http://email-api.internal/send", content).Result;

        // [PERF-2] HttpClient not disposed
        // [CORR-2] No error handling at all
    }

    // [ARCH-3] Crypto responsibility doesn't belong here
    // [SEC-8] Bad cryptography
    public string EncryptData(string data)
    {
        // [SEC-8] Hardcoded encryption key
        var key = Encoding.UTF8.GetBytes("0123456789ABCDEF");
        // [SEC-8] Hardcoded IV
        var iv = Encoding.UTF8.GetBytes("ABCDEF0123456789");

        // [SEC-8] Using ECB mode (insecure)
        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;  // [SEC-8] ECB mode - insecure!
        aes.Key = key;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var bytes = Encoding.UTF8.GetBytes(data);
        var encrypted = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
        return Convert.ToBase64String(encrypted);
    }

    // [ARCH-3] Auditing doesn't belong here
    public void AuditLog(string message)
    {
        // [SEC-4] Audit log contains sensitive data
        // [ANTI-PATTERN: Static abuse]
        GlobalState.Cache["lastAudit"] = message;

        // [CORR-4] File handle management issues
        var writer = new StreamWriter("C:\\logs\\audit.log", true);
        writer.WriteLine($"{DateTime.Now}: {message}"); // [CORR-7] DateTime.Now
        // [PERF-2][CORR-4] StreamWriter never disposed/closed!
    }

    // [ARCH-3] Reporting doesn't belong here
    public string GenerateUserReport()
    {
        var db = GetDbContext();

        // [PERF-7] Loading all data, no pagination
        var users = db.Users.Include(u => u.Orders).ToList();

        // [PERF-5] String concatenation in loop
        string report = "USER REPORT\n===========\n";
        foreach (var user in users)
        {
            report += $"User: {user.strUserName}, Email: {user.strEmail}, SSN: {user.SSN}\n"; // [SEC-4]
            report += $"  Password: {user.strPassword}\n"; // [SEC-4] Password in report!

            // [PERF-3] N+1 - query per user
            var orderCount = db.Orders.Where(o => o.CustomerID == user.user_ID).Count();
            report += $"  Orders: {orderCount}\n";
        }

        return report;
    }

    // [PERF-4] Collection operation anti-patterns
    public List<User> SearchUsers(string query)
    {
        var db = GetDbContext();

        // [PERF-4] Using List<T> when IReadOnlyList<T> suffices
        List<User> allUsers = db.Users.ToList();

        // [PERF-4] Multiple Where clauses that could be combined
        var results = allUsers
            .Where(u => u.strUserName != null)
            .Where(u => u.strUserName.Contains(query))
            .Where(u => u.boolIsActive)
            .ToList();

        // [PERF-4] Count() > 0 instead of Any()
        if (results.Count() > 0)
        {
            // [PERF-MINOR] Enum.Parse without caching
            var _ = Enum.Parse<DayOfWeek>("Monday");
        }

        return results;
    }

    // [CORR-3] Thread-unsafe operations on shared state
    public void IncrementCounter()
    {
        // [CORR-3] Race condition - should use Interlocked.Increment
        _operationCount = _operationCount + 1;

        // [CORR-3] Modifying collection while potentially being iterated
        lock (_userCache) // Wrong granularity
        {
            foreach (var key in _userCache.Keys.ToList())
            {
                if (_userCache[key].boolIsActive == false)
                    _userCache.Remove(key); // [CORR-3] Could cause issues
            }
        }
    }

    // [PERF-6] Boxing and allocation issues
    public ArrayList GetUserIds()
    {
        var db = GetDbContext();
        // [PERF-6] Value types in non-generic collection = boxing
        var result = new ArrayList();
        foreach (var user in db.Users.ToList())
        {
            result.Add(user.user_ID);       // Boxing int
            result.Add(user.boolIsActive);   // Boxing bool
            result.Add(user.Balance);        // Boxing decimal
        }
        return result;
    }

    // [ANTI-PATTERN: Commented-out code]
    // public void OldMethod()
    // {
    //     // This was the old way of doing things
    //     var x = 1 + 2;
    //     return x;
    // }

    // [ANTI-PATTERN: Leaky abstraction]
    public IQueryable<User> GetUsersQueryable()
    {
        var db = GetDbContext();
        // [CLEAN-3][DA-2] Repository returning IQueryable (leaky abstraction)
        return db.Users.AsQueryable();
    }
}
