// [ANTI-PATTERN: Static abuse]
// [PERF-5] String operations anti-patterns
// [PERF-MINOR] Regex without Compiled, DateTime.Now
// [NAMING] Wrong conventions

using System.Text.RegularExpressions;

namespace App.Helpers;

// [ANTI-PATTERN: Static abuse] - untestable, hidden dependencies
// [NAMING] Not following conventions
public static class StringHelper
{
    // [PERF-MINOR] Regex without RegexOptions.Compiled for reused pattern
    private static Regex emailRegex = new Regex(@"^[\w\.-]+@[\w\.-]+\.\w+$");

    // [PERF-MINOR] Another uncompiled regex
    private static Regex phoneRegex = new Regex(@"^\d{3}-\d{3}-\d{4}$");

    // [PERF-5] Case-insensitive comparison without StringComparison
    public static bool ContainsIgnoreCase(string source, string search)
    {
        // [PERF-5] ToLower() allocates new string instead of using StringComparison
        return source.ToLower().Contains(search.ToLower());
    }

    // [PERF-5] String concatenation in loop
    public static string RepeatString(string input, int count)
    {
        string result = "";
        for (int i = 0; i < count; i++)
        {
            result += input; // [PERF-5] Should use StringBuilder
        }
        return result;
    }

    // [CORR-7] DateTime issues
    public static string GetTimestamp()
    {
        // [CORR-7][PERF-MINOR] DateTime.Now instead of DateTimeOffset.UtcNow
        return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }

    // [CORR-7] Time zone assumptions
    public static bool IsBusinessHours()
    {
        // [CORR-7] Assumes local timezone
        var now = DateTime.Now; // Not UTC, not DateTimeOffset
        return now.Hour >= 9 && now.Hour < 17; // Timezone assumption
    }

    // [ANTI-PATTERN: Magic strings/numbers]
    public static string FormatCurrency(decimal amount)
    {
        if (amount > 1000000) return "Very Expensive";   // Magic number
        if (amount > 10000) return "Expensive";          // Magic number
        if (amount > 100) return "Moderate";             // Magic number
        return "Cheap";                                  // Magic string
    }

    // [NAMING] Non-PascalCase method
    public static string trim_and_lower(string input)
    {
        return input.Trim().ToLower();
    }

    // [NAMING] Abbreviation not following guidelines
    public static string GetUserID(string username)
    {
        // Should be GetUserId, not GetUserID
        return $"USR_{username.GetHashCode()}";
    }
}
