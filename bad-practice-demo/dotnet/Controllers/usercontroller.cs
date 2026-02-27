// ============================================================================
// [ARCH-3] GOD CONTROLLER - violates SRP massively
// [ARCH-2] Business logic in controller
// [CLEAN-4] Direct domain entity manipulation, DbContext in controller
// [SEC-1] SQL Injection
// [SEC-3] Missing [Authorize], hardcoded credentials
// [SEC-4] Logging & returning sensitive data
// [SEC-6] Path traversal
// [SEC-9] No input validation
// [API-1] REST violations (GET modifies state)
// [API-2] Stack traces in error responses
// [API-3] No API versioning
// [API-4] No pagination, circular references
// [API-5] No XML documentation, no [ProducesResponseType]
// [PERF-1] Sync-over-async (.Result, async void, Task.Run wrapping async)
// [PERF-2] Memory leaks (no disposal, static references)
// [PERF-3] N+1 queries
// [PERF-4] Collection anti-patterns
// [PERF-5] String concatenation in loops
// [PERF-7] No AsNoTracking, no pagination
// [CORR-1] Null dereference risks
// [CORR-2] Empty catch, throw ex, catching Exception
// [CORR-3] Thread safety issues
// [CORR-4] HttpClient per request, no disposal
// [CORR-6] Async correctness issues
// [NAMING] Wrong file name (should be UserController.cs but lowercase 'u')
// [PROJ-6] Random member ordering
// ============================================================================

using App.Models;
using App.Services;
using App.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;

namespace App.Controllers;

// [SEC-3] No [Authorize] attribute on sensitive endpoints
// [API-3] No API versioning
// [API-5] No XML documentation
// Missing [ProducesResponseType] attributes
[ApiController]
[Route("api/Users")]  // [API-MINOR] Inconsistent naming, hardcoded string in route
public class UserController : ControllerBase
{
    // [PROJ-6] Random member ordering - fields, then properties, then constructor should be together
    private static List<string> _auditLog = new List<string>(); // [CORR-3] Non-thread-safe static collection

    public string ConnectionString = "Server=prod-db-server.company.com;Database=ProductionDB;User Id=sa;Password=P@ssw0rd123!"; // [SEC-3] Hardcoded credentials

    private readonly UserService _userService;
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _dbContext; // [CLEAN-4] DbContext in controller

    // [ARCH-1] Constructor with too many dependencies (god class indicator)
    public UserController(
        UserService userService,
        IConfiguration configuration,
        AppDbContext dbContext)
    {
        _userService = userService;
        _configuration = configuration;
        _dbContext = dbContext;
    }

    // ========== [API-1] GET endpoint that modifies state ==========
    // [PERF-7] No AsNoTracking, loads entire table, no pagination
    // [SEC-9] No input validation
    // [API-4] No pagination on collection endpoint
    [HttpGet]
    public IActionResult GetAllUsers()
    {
        // [CORR-3] Modifying shared static state without synchronization
        GlobalState.RequestCount++;
        GlobalState.ActiveUsers.Add("anonymous");

        // [PERF-7] Loading entire entities when projection would suffice
        // [PERF-7] No AsNoTracking for read-only query
        var users = _dbContext.Users.Include(u => u.Orders).ToList();

        // [PERF-4] Count() > 0 instead of Any()
        if (users.Count() > 0)
        {
            // [SEC-4] Logging sensitive data
            foreach (var user in users)
            {
                Console.WriteLine($"User: {user.strUserName}, Password: {user.strPassword}, SSN: {user.SSN}");
            }
        }

        // [API-1] GET modifying state - writing audit log
        _auditLog.Add($"GetAllUsers called at {DateTime.Now}"); // [CORR-7] DateTime.Now instead of UtcNow

        // [SEC-4] Returning sensitive data (passwords, SSN, credit cards) in API response
        // [CLEAN-2] Returning domain entities directly
        return Ok(users);
    }

    // ========== [SEC-1] SQL Injection ==========
    [HttpGet("search")]
    public IActionResult SearchUsers(string name, string email) // [SEC-9] No [FromQuery] explicit binding
    {
        // [SEC-1] SQL Injection via string concatenation
        var query = "SELECT * FROM tbl_Users WHERE strUserName LIKE '%" + name + "%' AND strEmail = '" + email + "'";

        // [PERF-1] Sync-over-async: .Result blocks the thread
        var result = _dbContext.Users.FromSqlRaw(query).ToListAsync().Result;

        // [CORR-1] No null check on result
        return Ok(result);
    }

    // ========== [SEC-1] More SQL Injection + [DA-4] Dapper-style ==========
    [HttpGet("search-advanced/{searchTerm}")]
    public IActionResult AdvancedSearch(string searchTerm)
    {
        // [SEC-1] SQL injection via interpolation in raw SQL
        var sql = $"SELECT * FROM Users WHERE Name = '{searchTerm}' OR Email = '{searchTerm}'";
        var users = _dbContext.Users.FromSqlRaw(sql).ToList();
        return Ok(users);
    }

    // ========== [PERF-1] Async anti-patterns ==========
    // [PERF-1] async void - fire and forget, exception kills process
    [HttpPost("notify")]
    public async void NotifyUsers() // [PERF-1] async void!
    {
        // [CORR-4] New HttpClient per request - socket exhaustion
        var client = new HttpClient(); // [PERF-2] No disposal
        var users = _dbContext.Users.ToList();

        foreach (var user in users)
        {
            // [PERF-1] Task.Run wrapping already async method
            await Task.Run(async () =>
            {
                // [RES-3] No timeout, no retry, no circuit breaker
                var response = await client.PostAsync(
                    "http://notification-service/api/notify",
                    new StringContent(JsonConvert.SerializeObject(new { user.strEmail, user.strPassword })) // [SEC-4] Sending password
                );
            });
        }
        // [PERF-2] HttpClient never disposed
    }

    // ========== [SEC-6] Path Traversal ==========
    [HttpGet("avatar/{fileName}")]
    public IActionResult GetAvatar(string fileName)
    {
        // [SEC-6] User input directly in file path without validation
        var filePath = Path.Combine("C:\\uploads\\avatars", fileName);
        // No Path.GetFullPath validation, no base path check

        // [CORR-4] FileStream not disposed
        var stream = new FileStream(filePath, FileMode.Open);
        // [PERF-2] File handle never closed
        var bytes = new byte[stream.Length];
        stream.Read(bytes, 0, bytes.Length);
        // Missing: stream.Dispose()

        return File(bytes, "image/png");
    }

    // ========== [CORR-2] Exception handling disasters ==========
    [HttpGet("{id}")]
    public IActionResult GetUser(int id)
    {
        try
        {
            // [PERF-1] .GetAwaiter().GetResult() - sync over async
            var user = _dbContext.Users
                .Include(u => u.Orders)
                .FirstOrDefaultAsync(u => u.user_ID == id)
                .GetAwaiter().GetResult();

            // [CORR-1] Possible null dereference
            var name = user.strUserName.ToUpper(); // user could be null!

            // [SEC-4] Returning sensitive data
            return Ok(user);
        }
        catch (Exception) // [CORR-2] Catching Exception without logging
        {
            // [CORR-2] Empty catch block - swallowing exception
        }

        try
        {
            var user = _dbContext.Users.Find(id);
            return Ok(user);
        }
        catch (Exception ex)
        {
            // [CORR-2] throw ex loses stack trace
            throw ex;
        }
    }

    // ========== [PERF-3] N+1 Query Problem ==========
    [HttpGet("with-orders")]
    public IActionResult GetUsersWithOrders()
    {
        // [PERF-3] Missing .Include() - triggers lazy loading / N+1
        var users = _dbContext.Users.ToList();

        var result = new StringBuilder();
        foreach (var user in users)
        {
            // [PERF-3] Query inside loop - classic N+1
            var orders = _dbContext.Orders.Where(o => o.CustomerID == user.user_ID).ToList();

            // [PERF-5] String concatenation in loop instead of StringBuilder used properly
            var orderInfo = "";
            foreach (var order in orders)
            {
                orderInfo = orderInfo + "Order: " + order.OrderID + ", Amount: " + order.TotalAmount + "; ";
            }
            result.Append(user.strUserName + ": " + orderInfo);
        }

        return Ok(result.ToString());
    }

    // ========== [PERF-4] Collection anti-patterns ==========
    [HttpGet("stats")]
    public IActionResult GetStats()
    {
        // [PERF-4] Multiple enumerations of IEnumerable
        IEnumerable<User> users = _dbContext.Users.AsEnumerable();

        var count = users.Count();           // First enumeration
        var active = users.Where(u => u.boolIsActive).Count(); // Second enumeration
        var names = users.Select(u => u.strUserName).ToList();  // Third enumeration

        // [PERF-4] Count() > 0 instead of Any()
        var hasAdmins = users.Where(u => u.Role == "Admin").Count() > 0;

        // [PERF-4] FirstOrDefault then null check instead of Any with predicate
        var firstAdmin = users.Where(u => u.Role == "Admin").FirstOrDefault();
        var adminExists = firstAdmin != null;

        // [PERF-4] Multiple Where that could be combined
        var filtered = users.Where(u => u.boolIsActive).Where(u => u.Role == "Admin").Where(u => u.Balance > 0);

        // [PERF-6] Boxing: value type in non-generic collection
        System.Collections.ArrayList boxingList = new System.Collections.ArrayList();
        foreach (var user in _dbContext.Users.ToList())
        {
            boxingList.Add(user.user_ID);      // Boxing int to object
            boxingList.Add(user.boolIsActive);  // Boxing bool to object
        }

        return Ok(new { count, active, hasAdmins, adminExists });
    }

    // ========== [SEC-3][SEC-9] Auth issues + [API-1] POST without anti-forgery ==========
    // [SEC-3] AllowAnonymous on sensitive endpoint
    [AllowAnonymous]
    [HttpPost("login")]
    public IActionResult Login(string username, string password) // [SEC-9] No [FromBody], no validation
    {
        // [SEC-3] Hardcoded credentials
        if (username == "admin" && password == "admin123")
        {
            // [SEC-3] JWT without expiration
            var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.static-token-no-expiry";
            // [SEC-4] Logging credentials
            Console.WriteLine($"Login successful for {username} with password {password}");
            return Ok(new { Token = token, Password = password }); // [SEC-4] Returning password
        }

        // [SEC-4] Sensitive data in query string (visible in logs/history)
        return Redirect($"/api/Users/login?username={username}&password={password}&retry=true");
    }

    // ========== [SEC-8] Bad Cryptography ==========
    [HttpPost("hash-password")]
    public IActionResult HashPassword(string password)
    {
        // [SEC-8] Using MD5 for security purposes
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Ok(Convert.ToBase64String(hash));
    }

    // ========== [DA-1] SaveChanges in loop ==========
    [HttpPost("bulk-update")]
    public IActionResult BulkUpdate([FromBody] List<UserDto> users)
    {
        // [DA-1] No transaction for multi-entity operation
        foreach (var dto in users)
        {
            var user = _dbContext.Users.Find(dto.Id);
            if (user != null)
            {
                user.strUserName = dto.Name;
                user.strEmail = dto.Email;
                // [DA-1] SaveChanges in loop!
                _dbContext.SaveChanges();
            }
        }

        return Ok("Updated");
    }

    // ========== [PERF-5] String operations ==========
    [HttpGet("export")]
    public IActionResult ExportUsers()
    {
        var users = _dbContext.Users.ToList();

        // [PERF-5] String concatenation in loop (should use StringBuilder)
        string csv = "Id,Name,Email,Password,SSN\n"; // [SEC-4] Exporting sensitive data
        foreach (var user in users)
        {
            csv += user.user_ID + "," + user.strUserName + "," + user.strEmail + "," + user.strPassword + "," + user.SSN + "\n";
        }

        // [PERF-5] Case-insensitive comparison without StringComparison
        if (csv.ToLower().Contains("admin"))
        {
            Console.WriteLine("Admin found in export");
        }

        return Content(csv, "text/csv");
    }

    // ========== [SEC-5] Insecure Deserialization ==========
    [HttpPost("import")]
    public IActionResult ImportData([FromBody] string base64Data)
    {
        // [SEC-5] BinaryFormatter deserialization - critical RCE vulnerability
        #pragma warning disable SYSLIB0011
        var formatter = new BinaryFormatter();
        #pragma warning restore SYSLIB0011
        var bytes = Convert.FromBase64String(base64Data);
        using var stream = new MemoryStream(bytes);
        // Deserializing untrusted input with BinaryFormatter!
        try
        {
            #pragma warning disable SYSLIB0011
            var obj = formatter.Deserialize(stream);
            #pragma warning restore SYSLIB0011
            return Ok(obj);
        }
        catch (Exception ex)
        {
            // [API-2] Stack traces in production error responses
            return StatusCode(500, new { Error = ex.Message, StackTrace = ex.StackTrace });
        }
    }

    // ========== [CORR-6] Return null instead of Task.CompletedTask ==========
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _dbContext.Users.FindAsync(id);
        if (user == null)
        {
            // [CORR-6] Missing cancellation token propagation
            return NotFound();
        }

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync(); // [CORR-6] No CancellationToken

        // [CORR-6] Fire and forget without exception handling
        _ = SendDeletionNotification(user.strEmail);

        return NoContent();
    }

    // [CORR-6] Missing cancellation token
    private async Task SendDeletionNotification(string email)
    {
        // [CORR-4] New HttpClient per call
        var client = new HttpClient();
        await client.PostAsync("http://email-service/notify", new StringContent(email));
        // No error handling, no timeout, no disposal
    }

    // ========== [SEC-2] XSS vulnerability ==========
    [HttpGet("profile-html/{id}")]
    public IActionResult GetProfileHtml(int id)
    {
        var user = _dbContext.Users.Find(id);
        // [SEC-2] Rendering user-controlled input as raw HTML
        var html = $"<html><body><h1>Welcome {user?.strUserName}</h1><div>{user?.Notes()}</div></body></html>";
        return Content(html, "text/html");
    }

    // ========== [ANTI-PATTERN: Deep nesting] ==========
    [HttpPost("process")]
    public IActionResult ProcessUser([FromBody] UserDto dto)
    {
        // [ANTI-PATTERN: Deep nesting instead of guard clauses]
        if (dto != null)
        {
            if (dto.Name != null)
            {
                if (dto.Email != null)
                {
                    if (dto.Email.Contains("@"))
                    {
                        if (dto.Id > 0)
                        {
                            var user = _dbContext.Users.Find(dto.Id);
                            if (user != null)
                            {
                                user.strUserName = dto.Name;
                                user.strEmail = dto.Email;
                                _dbContext.SaveChanges();
                                return Ok(user);
                            }
                            else
                            {
                                return NotFound();
                            }
                        }
                        else
                        {
                            return BadRequest("Invalid ID");
                        }
                    }
                    else
                    {
                        return BadRequest("Invalid email");
                    }
                }
                else
                {
                    return BadRequest("Email required");
                }
            }
            else
            {
                return BadRequest("Name required");
            }
        }
        else
        {
            return BadRequest("Body required");
        }
    }

    // ========== [ANTI-PATTERN: Boolean parameter] ==========
    [HttpGet("list")]
    public IActionResult ListUsers(bool includeInactive, bool includeOrders, bool sortByName, bool descending)
    {
        // [ANTI-PATTERN: Boolean parameters] - unclear call sites
        // [ANTI-PATTERN: Magic numbers]
        var query = _dbContext.Users.AsQueryable();

        if (!includeInactive)
            query = query.Where(u => u.boolIsActive);
        if (includeOrders)
            query = query.Include(u => u.Orders);
        if (sortByName)
            query = descending ? query.OrderByDescending(u => u.strUserName) : query.OrderBy(u => u.strUserName);

        // [ANTI-PATTERN: Magic number]
        var users = query.Take(999999).ToList(); // Unbounded query
        return Ok(users);
    }
}

// [ANTI-PATTERN: Extension method class in controller file]
public static class UserExtensions
{
    // [ANTI-PATTERN: Train wreck / Law of Demeter violation]
    public static string Notes(this User user)
    {
        return user.Manager.Orders.First().Items.First().ProductName;
    }
}
