// [BG-SVC] Background service with ALL violations:
// - Unhandled exceptions killing host
// - Missing cancellation token respect
// - Scoped services in singleton
// - No graceful shutdown
// - Tight loop without delay
// - Missing health checks
// - No retry logic
// - Direct DbContext injection

using App.Models;
using Microsoft.EntityFrameworkCore;

namespace App.BackgroundServices;

// [BG-SVC] Direct DbContext injection in BackgroundService (singleton!)
// Should use IServiceScopeFactory
public class DataSyncService : BackgroundService
{
    // [ARCH-1] Captive dependency: Singleton service holding scoped DbContext
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _config;

    // [BG-SVC] Direct DbContext injection (wrong! needs IServiceScopeFactory)
    public DataSyncService(AppDbContext dbContext, IConfiguration config)
    {
        _dbContext = dbContext;
        _config = config;
    }

    // [BG-SVC] StartAsync with long-running work
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        // [BG-SVC] Blocking operation in StartAsync
        Thread.Sleep(5000); // [PERF-1] Blocking thread!

        // [SEC-4] Logging connection string at startup
        Console.WriteLine($"DataSync starting with connection: {_config.GetConnectionString("DefaultConnection")}");

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // [BG-SVC] No try-catch - unhandled exception will kill the host!
        // [BG-SVC] Missing cancellation token respect

        while (true) // [BG-SVC] Not checking stoppingToken!
        {
            // [BG-SVC] Tight loop without delay (will spin CPU)
            // (adding minimal delay to prevent actual CPU spinning in demo, but it's still wrong)

            // [PERF-1] Sync-over-async
            var users = _dbContext.Users.ToList();

            foreach (var user in users)
            {
                // [PERF-3] N+1 query in background service
                var orders = _dbContext.Orders.Where(o => o.CustomerID == user.user_ID).ToList();

                // [CORR-3] Modifying shared static state
                GlobalState.Cache[$"user_{user.user_ID}_orders"] = orders.Count;
            }

            // [CORR-4] New HttpClient per iteration
            var client = new HttpClient();
            try
            {
                // [RES-3] No timeout, no circuit breaker
                var response = client.GetAsync("http://external-sync-service/api/sync").Result; // [PERF-1] .Result

                // [CORR-1] No null check
                var content = response.Content.ReadAsStringAsync().Result;
            }
            catch (Exception ex)
            {
                // [BG-SVC] No retry logic for transient failures
                // [CORR-2] Catching generic Exception
                // [LOG] Console.WriteLine instead of ILogger
                Console.WriteLine($"Sync error: {ex.Message}");
                // Continues loop immediately - no backoff
            }

            // [PERF-2] HttpClient never disposed

            // [BG-SVC] Small delay but still essentially a tight loop
            await Task.Delay(100); // Should be configurable, much longer
        }
    }

    // [BG-SVC] Missing graceful shutdown
    public override Task StopAsync(CancellationToken cancellationToken)
    {
        // No cleanup, no waiting for current operation to complete
        Console.WriteLine("DataSync stopping (no graceful shutdown)");
        return Task.CompletedTask;
    }
}
