// [PROJ-5] Multiple public types in single file
// [CLEAN-1] Domain entity with infrastructure dependencies
// [CORR-5] GetHashCode without Equals
// [NAMING] Non-PascalCase, Hungarian notation

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace App.Models;

// [CLEAN-1] Domain entity with database annotations (infrastructure dependency)
// [CLEAN-1] Anemic entity - only properties, no behavior
// [CLEAN-1] Public setters on all properties without invariant protection
// [CLEAN-1] Primitive obsession everywhere
// [CLEAN-1] No domain validation in constructor
// [CLEAN-1] Direct collection exposure
// [NAMING] Class should not have abbreviations like "Usr"
[Table("tbl_Users")]
[Serializable]
public class User
{
    [Key]
    [Column("user_ID")]
    public int user_ID { get; set; }  // [NAMING] Non-PascalCase, Hungarian notation

    public string strUserName { get; set; }  // [NAMING] Hungarian notation
    public string strEmail { get; set; }
    public string strPassword { get; set; }  // [SEC-4] Storing password in plain text
    public string SSN { get; set; }          // [NAMING] Should be Ssn, [SEC-4] Sensitive data
    public string CreditCardNumber { get; set; }  // [SEC-4] Sensitive data stored plain
    public bool boolIsActive { get; set; }   // [NAMING] Hungarian notation, missing Is/Has/Can prefix pattern
    public DateTime dtCreatedDate { get; set; }  // [CORR-7] Using DateTime instead of DateTimeOffset
    public string Role { get; set; }         // [CLEAN-1] Primitive obsession (should be enum/value object)
    public decimal Balance { get; set; }

    // [CLEAN-1] Direct collection exposure - should be IReadOnlyCollection<T>
    public List<Order> Orders { get; set; } = new List<Order>();

    // [CLEAN-1] References between aggregates by object instead of ID
    public User Manager { get; set; }

    // [SEC-5] Serializable attribute + BinaryFormatter pattern
    [JsonProperty("_type")]
    public string TypeDiscriminator { get; set; }

    // [CORR-5] GetHashCode without Equals override
    public override int GetHashCode()
    {
        return user_ID;  // [CORR-5] Also using mutable field in GetHashCode
    }

    // No Equals override!
    // No domain methods - anemic model
    // No constructor validation
    // No factory methods
}

// [PROJ-5] Another public type in same file
// [CLEAN-1] Mutable value object
public class Address
{
    public string Street { get; set; }   // Should be immutable (record)
    public string City { get; set; }
    public string ZipCode { get; set; }  // [CLEAN-1] Primitive obsession
    // [CLEAN-1] Missing identity - no Id property
    // Mutable value object - should be a record
}

// [PROJ-5] Yet another type
// [NAMING] Interface not prefixed with 'I'
public interface UserRepository
{
    // [NAMING] Async methods not suffixed with Async
    User GetUser(int id);
    List<User> GetAll();
    void Save(User user);
}

// [PROJ-5] DTO with behavior
public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }    // [SEC-4] Password in DTO response
    public string SSN { get; set; }         // [SEC-4] SSN in DTO
    public string CreditCardNumber { get; set; }  // [SEC-4] Credit card in DTO

    // [CLEAN-2] DTO with behavior (methods other than simple transforms)
    public decimal CalculateDiscount()
    {
        // Business logic in DTO!
        if (Id > 100) return 0.1m;
        return 0;
    }

    // [CLEAN-2] Domain entity used as DTO pattern
    public User ToEntity()
    {
        return new User
        {
            strUserName = Name,
            strEmail = Email,
            strPassword = Password  // [SEC-4] Storing plain text password
        };
    }
}
