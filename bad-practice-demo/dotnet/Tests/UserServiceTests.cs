// [TEST-1] Tests with no assertions, tautological tests
// [TEST-2] Missing edge cases, testing implementation not behavior
// [TEST-3] Non-descriptive names, no categories
// [TEST-4] Hardcoded ports/connection strings, no isolation
// [PROJ-5] Test file in wrong location (should be separate project)

using App.Models;
using App.Services;
using App.Helpers;

namespace App.Tests;

// [TEST-4] Tests in same project as production code!
// [TEST-3] Non-descriptive class name
public class Tests
{
    // [TEST-3] Non-descriptive test names
    // [TEST-1] Test with no assertions
    public void Test1()
    {
        var user = new User();
        user.strUserName = "test";
        // [TEST-1] No assertion at all!
    }

    // [TEST-1] Tautological test - always passes
    public void Test2()
    {
        var result = true;
        if (result != true)
            throw new Exception("Should not happen"); // This literally can never fail
    }

    // [TEST-2] Testing implementation details instead of behavior
    public void Test3()
    {
        var user = new User();
        user.strUserName = "John";
        user.strEmail = "john@example.com";

        // Testing internal property setting, not behavior
        if (user.strUserName != "John") throw new Exception("Name mismatch");
        if (user.strEmail != "john@example.com") throw new Exception("Email mismatch");
    }

    // [TEST-2] Missing edge case coverage (null, empty, boundary)
    public void TestHashPassword()
    {
        var hash = CryptoHelper.HashPassword("password123");
        // Only tests one case, no null/empty/boundary tests
        if (hash == null) throw new Exception("Hash is null");
    }

    // [TEST-1] Test that modifies shared state without cleanup
    public void TestGlobalState()
    {
        // [TEST-1] Modifying shared state
        GlobalState.ActiveUsers.Add("test-user");
        GlobalState.RequestCount = 999;
        // No cleanup! This affects other tests

        // [TEST-2] Hard-coded magic values without explanation
        if (GlobalState.RequestCount != 999) throw new Exception("fail");
    }

    // [TEST-4] Integration test with hardcoded connection string
    public void TestDatabaseConnection()
    {
        // [TEST-4] Hardcoded connection string
        var connectionString = "Server=localhost,1433;Database=TestDb;User Id=sa;Password=TestPass123!";
        // [TEST-4] Depending on external service availability
        // [TEST-4] No WebApplicationFactory usage
        // This will fail if SQL Server is not running
    }

    // [TEST-3] Duplicate test setup code (not using fixtures)
    public void TestUserCreation1()
    {
        // Duplicated setup
        var user = new User
        {
            strUserName = "test",
            strEmail = "test@test.com",
            strPassword = "pass",
            boolIsActive = true
        };
        if (user.strUserName != "test") throw new Exception("fail");
    }

    public void TestUserCreation2()
    {
        // [TEST-3] Same duplicated setup
        var user = new User
        {
            strUserName = "test",
            strEmail = "test@test.com",
            strPassword = "pass",
            boolIsActive = true
        };
        if (user.strEmail != "test@test.com") throw new Exception("fail");
    }

    // [TEST-1] Test calling production external service
    public async Task TestExternalService()
    {
        var client = new HttpClient();
        // [TEST-1] Calling actual production service!
        var response = await client.GetAsync("https://api.production.company.com/health");
        // No mock, no test double
    }

    // [TEST-2] Overly complex test setup
    public void TestComplexScenario()
    {
        // [TEST-2] This test setup is more complex than the code it tests
        var user1 = new User { user_ID = 1, strUserName = "A", strEmail = "a@a.com", strPassword = "p1", Role = "admin" };
        var user2 = new User { user_ID = 2, strUserName = "B", strEmail = "b@b.com", strPassword = "p2", Role = "user" };
        var user3 = new User { user_ID = 3, strUserName = "C", strEmail = "c@c.com", strPassword = "p3", Role = "admin" };
        var order1 = new Order { OrderID = 1, CustomerID = 1, TotalAmount = 100, Status = "pending" };
        var order2 = new Order { OrderID = 2, CustomerID = 1, TotalAmount = 200, Status = "shipped" };
        var order3 = new Order { OrderID = 3, CustomerID = 2, TotalAmount = 300, Status = "pending" };

        user1.Orders.Add(order1);
        user1.Orders.Add(order2);
        user2.Orders.Add(order3);

        // After all that setup, a trivial assertion
        if (user1.Orders.Count != 2) throw new Exception("fail");
    }
}
