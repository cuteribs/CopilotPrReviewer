// [PROJ-5] Multiple types in single file
// [CLEAN-1] Infrastructure dependencies in domain model
// [CORR-7] DateTime instead of DateTimeOffset
// [NAMING] Wrong conventions

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace App.Models;

[Table("tbl_Orders")]
public class Order
{
    [Key]
    public int OrderID { get; set; }  // [NAMING] Should be OrderId not OrderID

    // [CLEAN-1] Aggregate references by object instead of ID
    public User Customer { get; set; }
    public int CustomerID { get; set; }

    // [CORR-7] Using DateTime instead of DateTimeOffset
    public DateTime OrderDate { get; set; }
    public DateTime? ShippedDate { get; set; }

    // [CLEAN-1] Public setters, no invariant protection
    public string Status { get; set; }     // [CLEAN-1] Primitive obsession - should be enum/value object
    public decimal TotalAmount { get; set; }
    public string Notes { get; set; }

    // [CLEAN-1] Direct collection exposure
    public List<OrderItem> Items { get; set; } = new List<OrderItem>();

    // [CLEAN-1] No domain methods for state changes
    // Anemic model - all behavior is in services
}

// [PROJ-5] Multiple types in file
public class OrderItem
{
    public int ID { get; set; }    // [NAMING] Should be Id
    public int OrderID { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    // [CLEAN-1] Child entity accessible without going through aggregate root
    [JsonIgnore]
    public Order Order { get; set; }
}

// [PROJ-5] Another type in same file
// Enum with no documentation
public enum order_status  // [NAMING] Non-PascalCase enum name
{
    pending = 0,        // [NAMING] Non-PascalCase enum value
    processing = 1,
    shipped = 2,
    DELIVERED = 3,      // [NAMING] Inconsistent casing
    cancelled = 4
}

// [PROJ-5] Config class here too - wrong location
// [CONFIG-1] Mutable options class, no validation
public class AppConfig
{
    public string DbConnectionString { get; set; }
    public string ApiKey { get; set; }
    public int Timeout { get; set; }
    // No [Required] attributes
    // No validation
    // Mutable
}
