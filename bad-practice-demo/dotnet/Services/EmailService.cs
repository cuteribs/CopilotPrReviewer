// [ARCH-3] Service with wrong responsibilities
// [CORR-4] Resource management issues
// [RES-1][RES-3] No resilience patterns

using System.Net.Http;
using System.Text;

namespace App.Services;

// [NAMING] Async methods not suffixed with Async
public class EmailService
{
    // [CORR-4] Static HttpClient field (proper) BUT created with new (wrong)
    // Actually let's make it worse:
    private readonly IServiceProvider _sp;

    public EmailService(IServiceProvider sp)
    {
        _sp = sp;
    }

    // [NAMING] Async method not suffixed with 'Async'
    // [CORR-6] Missing CancellationToken
    public async Task SendEmail(string to, string subject, string body)
    {
        // [CORR-4] Creating new HttpClient per call - socket exhaustion
        // [RES-3] No HttpClientFactory, no resilience policies
        var client = new HttpClient();
        // [RES-3] Infinite timeout
        client.Timeout = TimeSpan.FromHours(24);

        // [SEC-4] Logging email content which may contain sensitive data
        Console.WriteLine($"Sending email to {to}: {body}");

        try
        {
            // [RES-1] No retry, no circuit breaker, no timeout policy
            // [RES-1] Hardcoded URL
            var response = await client.PostAsync(
                "http://smtp-relay.internal:25/send",
                new StringContent($"{{\"to\":\"{to}\",\"subject\":\"{subject}\",\"body\":\"{body}\"}}", Encoding.UTF8, "application/json")
            );

            // [CORR-1] No null check on response
            // [CORR-2] No error handling for non-success status
        }
        catch (HttpRequestException)
        {
            // [CORR-2] Empty catch block - swallowing exception
            // [RES-1] No retry logic for transient failure
        }
        // [PERF-2] HttpClient never disposed
    }

    // [NAMING] Missing Async suffix
    // [PERF-1] async void!
    public async void SendBulkEmails(List<string> recipients, string subject, string body)
    {
        // [PERF-1] async void - exceptions will crash the process
        foreach (var recipient in recipients)
        {
            await SendEmail(recipient, subject, body);
            // [RES-1] No jitter, no backoff between requests
        }
    }

    // [CORR-6] Returns null instead of Task.CompletedTask
    public Task ValidateEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            // [CORR-6] Returning null instead of Task.CompletedTask
            return null;
        }

        Console.WriteLine($"Email {email} is valid");
        return Task.CompletedTask;
    }
}
