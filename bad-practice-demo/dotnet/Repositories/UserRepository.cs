// [SEC-1] SQL Injection throughout
// [DA-2] Business logic in repository
// [DA-4] Connections not disposed, no parameterization
// [DA-5] Manual mapping everywhere
// [CLEAN-3] Business logic in infrastructure
// [PERF-7] No AsNoTracking, unbounded queries

using App.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Data.Common;

namespace App.Repositories;

// [NAMING] Interface not prefixed with 'I' (see Models/User.cs - UserRepository interface)
public class UserRepository
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public UserRepository(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // [SEC-1] SQL Injection via string concatenation
    public List<User> SearchByName(string name)
    {
        // [SEC-1] Critical: SQL injection
        var sql = "SELECT * FROM tbl_Users WHERE strUserName LIKE '%" + name + "%'";
        return _db.Users.FromSqlRaw(sql).ToList();
    }

    // [SEC-1] Another SQL injection
    public User FindByEmail(string email)
    {
        // [SEC-1] SQL injection via interpolation in FromSqlRaw
        return _db.Users.FromSqlRaw($"SELECT * FROM tbl_Users WHERE strEmail = '{email}'").FirstOrDefault();
    }

    // [DA-2][CLEAN-3] Business logic in repository
    public User AuthenticateUser(string username, string password)
    {
        var user = _db.Users.FirstOrDefault(u => u.strUserName == username);

        // [CLEAN-3] Authentication logic belongs in domain/application, not infrastructure
        if (user == null) return null;

        // [SEC-3] Plain text password comparison - no hashing!
        if (user.strPassword != password) return null;

        // [CLEAN-3] More business logic - updating last login
        user.dtCreatedDate = DateTime.Now; // [CORR-7] Abusing CreatedDate as LastLogin + DateTime.Now

        // [DA-2] Business rule in repo
        if (user.Role == "admin")
        {
            GlobalState.ActiveUsers.Add(user.strUserName); // [ANTI-PATTERN: Static abuse]
        }

        _db.SaveChanges();
        return user;
    }

    // [PERF-7] Unbounded query, no pagination, no AsNoTracking
    public List<User> GetAllUsers()
    {
        // [PERF-7] Missing AsNoTracking for read-only
        // [PERF-7] No pagination - loads ALL users
        return _db.Users
            .Include(u => u.Orders)
            .Include(u => u.Manager)
            .ToList();
    }

    // [DA-1] Synchronous database calls
    public void UpdateMultipleUsers(List<User> users)
    {
        // [DA-1] No transaction for multi-entity operation
        foreach (var user in users)
        {
            var existing = _db.Users.Find(user.user_ID);
            if (existing != null)
            {
                existing.strUserName = user.strUserName;
                existing.strEmail = user.strEmail;
                // [DA-1] SaveChanges in loop
                _db.SaveChanges();
            }
        }
    }

    // [DA-5] Manual mapping when AutoMapper would be appropriate
    public UserDto MapToDto(User user)
    {
        // [DA-5] Manual tedious mapping
        return new UserDto
        {
            Id = user.user_ID,
            Name = user.strUserName,
            Email = user.strEmail,
            Password = user.strPassword, // [SEC-4] Mapping password to DTO
            SSN = user.SSN,             // [SEC-4] Mapping SSN to DTO
            CreditCardNumber = user.CreditCardNumber // [SEC-4] Mapping credit card
        };
    }

    // [CLEAN-3][DA-2] Report generation in repository!
    public string GenerateMonthlyReport(int month, int year)
    {
        // [CLEAN-3] This is business logic, not data access
        var users = _db.Users.Include(u => u.Orders).ToList();

        // [PERF-5] String concatenation
        string report = "";
        foreach (var user in users)
        {
            var monthlyOrders = user.Orders
                .Where(o => o.OrderDate.Month == month && o.OrderDate.Year == year);

            report += $"User: {user.strUserName}\n";
            report += $"  Total Orders: {monthlyOrders.Count()}\n"; // [PERF-4] Count() on IEnumerable
            report += $"  Total Revenue: {monthlyOrders.Sum(o => o.TotalAmount)}\n";

            // [DA-2] Applying business rules in repo
            if (monthlyOrders.Sum(o => o.TotalAmount) > 1000)
            {
                report += "  ** VIP CUSTOMER **\n";
                user.Role = "VIP"; // [DA-2] Modifying entity in report generation!
                _db.SaveChanges();
            }
        }

        return report;
    }
}
