// [ARCH-2] Business logic in controller
// [CLEAN-4] Direct infrastructure in presentation
// [API-1] REST violations
// [API-2] Inconsistent error responses
// [DA-1] Synchronous database calls in async context
// [DA-2] Business logic in repository calls from controller
// [PERF-8] No caching for expensive operations
// [CORR-7] DateTime handling issues
// [FM-1] Feature flags in wrong layer, static checks
// [ANTI-PATTERN: Temporal coupling]
// [ANTI-PATTERN: Regions in methods]

using App.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace App.Controllers;

// [SEC-3] No [Authorize]
// [API-3] No versioning
[ApiController]
[Route("api/order-management")]  // [API-MINOR] Inconsistent with UserController naming
public class OrderController : ControllerBase
{
    private readonly AppDbContext _dbContext;  // [CLEAN-4] Direct DbContext in controller
    private readonly IConfiguration _config;  // [CONFIG-1] Direct IConfiguration

    // [ANTI-PATTERN: Temporal coupling] - must call Initialize before using
    private bool _initialized = false;
    private List<Order> _cachedOrders;

    public OrderController(AppDbContext dbContext, IConfiguration config)
    {
        _dbContext = dbContext;
        _config = config;
    }

    // [ANTI-PATTERN: Temporal coupling] Must be called first
    [HttpPost("initialize")]
    public IActionResult Initialize()
    {
        _cachedOrders = _dbContext.Orders.ToList();
        _initialized = true;
        return Ok("Initialized");
    }

    // [API-1] GET endpoint that modifies state
    [HttpGet("create-order")]
    public IActionResult CreateOrderViaGet(int customerId, string product, decimal amount)
    {
        // [SEC-9] No input validation at all
        // [VAL-1][VAL-3][VAL-4] No validation anywhere

        #region Business Logic In Controller
        // [ANTI-PATTERN: Regions in methods - method too large]
        // [ARCH-2][CLEAN-4] All business logic directly in controller

        // [CORR-7] Using DateTime.Now instead of DateTimeOffset.UtcNow
        var order = new Order
        {
            CustomerID = customerId,
            OrderDate = DateTime.Now, // [PERF-MINOR] DateTime.Now instead of UtcNow
            Status = "pending",       // [ANTI-PATTERN: Magic string]
            TotalAmount = amount
        };

        // [FM-1] Feature flag check with static/hardcoded value in controller
        // [FM-1] Feature check scattered in domain logic
        bool enableDiscount = true;  // [FM-1] Hardcoded feature flag
        if (enableDiscount)
        {
            // [ARCH-2] Business logic in controller
            if (amount > 100)
                order.TotalAmount = amount * 0.9m;  // [ANTI-PATTERN: Magic number]
            else if (amount > 50)
                order.TotalAmount = amount * 0.95m; // [ANTI-PATTERN: Magic number]
        }

        _dbContext.Orders.Add(order);
        _dbContext.SaveChanges(); // [DA-1] Synchronous in what should be async

        // [CONFIG-3] Environment check in code instead of configuration
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production")
        {
            // hardcoded environment name
            Console.WriteLine("Production order created"); // [LOG] Console.WriteLine instead of ILogger
        }

        #endregion

        // [API-2] Inconsistent response format (compare with UserController)
        return new JsonResult(new { success = true, data = order, error = (string)null });
    }

    // [PERF-8] Expensive operation without caching
    [HttpGet("report")]
    public IActionResult GetOrderReport()
    {
        // [PERF-8] This is called frequently but recomputes everything each time
        // [PERF-7] Loading all entities
        var orders = _dbContext.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .ToList(); // [PERF-7] No AsNoTracking, no pagination

        // [PERF-MINOR] Regex without RegexOptions.Compiled
        var regex = new Regex(@"\d{4}-\d{2}-\d{2}");

        // [PERF-4] Multiple enumerations
        var totalRevenue = orders.Sum(o => o.TotalAmount);
        var avgOrder = orders.Average(o => o.TotalAmount);
        var ordersByStatus = orders.GroupBy(o => o.Status).ToDictionary(g => g.Key, g => g.Count());
        var topCustomers = orders.GroupBy(o => o.CustomerID).OrderByDescending(g => g.Sum(o => o.TotalAmount)).Take(10);

        // [CORR-7] Date comparison without considering time component
        var todayOrders = orders.Where(o => o.OrderDate.Date == DateTime.Now.Date).ToList();

        return Ok(new { totalRevenue, avgOrder, ordersByStatus, topCustomers, todayOrders });
    }

    // [API-2] Error handling that returns inconsistent format + stack traces
    [HttpPut("{id}")]
    public IActionResult UpdateOrder(int id, [FromBody] Order order)
    {
        try
        {
            // [CLEAN-2] Domain entity received directly from API (no DTO)
            var existing = _dbContext.Orders.Find(id);

            // [CORR-1] No null check - will throw if not found
            existing.Status = order.Status;
            existing.TotalAmount = order.TotalAmount;
            existing.Notes = order.Notes;

            _dbContext.SaveChanges();

            // [API-2] Returns raw entity, not DTO
            return Ok(existing);
        }
        catch (NullReferenceException)
        {
            // [API-2] Inconsistent error format
            return NotFound("Order not found");
        }
        catch (Exception ex)
        {
            // [API-2] Stack trace in production + [CORR-2] Catching generic Exception
            return StatusCode(500, $"Error: {ex.Message}\n{ex.StackTrace}");
        }
    }

    // [PERF-5] String operations anti-patterns
    [HttpGet("summary")]
    public IActionResult GetSummary()
    {
        var orders = _dbContext.Orders.Include(o => o.Items).ToList();

        // [PERF-5] String.Format in hot path
        string summary = "";
        foreach (var order in orders)
        {
            // [PERF-5] String concatenation in loop
            summary += String.Format("Order #{0} - {1} items - ${2}\n",
                order.OrderID, order.Items.Count, order.TotalAmount);

            foreach (var item in order.Items)
            {
                summary += "\t" + item.ProductName + " x" + item.Quantity + " @ $" + item.UnitPrice + "\n";
            }
        }

        // [PERF-5] ToLower() without StringComparison
        if (summary.ToLower().Contains("cancelled"))
        {
            summary += "\n*** CONTAINS CANCELLED ORDERS ***";
        }

        return Content(summary, "text/plain");
    }

    // [SER-1][SER-2] Serialization issues
    [HttpPost("deserialize")]
    public IActionResult DeserializeOrder([FromBody] string json)
    {
        // [SER-1] No JsonSerializerOptions configuration
        // [SER-3] Newtonsoft with default settings
        var settings = new JsonSerializerSettings
        {
            // [SEC-5][SER-3] TypeNameHandling.All = RCE vulnerability
            TypeNameHandling = TypeNameHandling.All,
            // [SER-3] DateTimeZoneHandling not configured
        };

        try
        {
            // [SER-1] Ignoring serialization errors silently
            var order = JsonConvert.DeserializeObject<Order>(json, settings);
            return Ok(order);
        }
        catch
        {
            // [CORR-2] Empty catch, silently swallowing
            return Ok(new Order());
        }
    }

    // [SEC-9] Accepting unbounded collection
    [HttpPost("bulk-create")]
    public IActionResult BulkCreate([FromBody] List<Order> orders) // [SEC-9] No size limit
    {
        // [DA-1] SaveChanges in loop instead of batch
        foreach (var order in orders)
        {
            _dbContext.Orders.Add(order);
            _dbContext.SaveChanges(); // [DA-1] SaveChanges per iteration
        }

        return Ok($"Created {orders.Count} orders");
    }
}
