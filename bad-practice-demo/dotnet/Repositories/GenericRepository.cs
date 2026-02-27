// [DA-2] Business logic in repository
// [DA-2] Generic repository anti-pattern hiding EF capabilities
// [DA-4] SQL injection, connections not disposed
// [CLEAN-3] Business logic in infrastructure
// [ANTI-PATTERN: Generic Repository]
// [SEC-1] SQL injection

using App.Models;
using Microsoft.EntityFrameworkCore;

namespace App.Repositories;

// [ANTI-PATTERN: Generic Repository] Hides EF Core capabilities
// [NAMING] Generic type parameter not prefixed with T
public class GenericRepository
{
    // [ARCH-1] Service Locator
    private readonly IServiceProvider _serviceProvider;

    public GenericRepository(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public List<E> GetAll<E>() where E : class  // [NAMING] 'E' not prefixed with 'T'
    {
        // [ARCH-1] Service Locator pattern
        var db = _serviceProvider.GetService(typeof(AppDbContext)) as AppDbContext;
        // [PERF-7] No AsNoTracking
        return db.Set<E>().ToList();
    }

    public E GetById<E>(int id) where E : class
    {
        var db = _serviceProvider.GetService(typeof(AppDbContext)) as AppDbContext;
        return db.Set<E>().Find(id);
    }

    public void Add<E>(E entity) where E : class
    {
        var db = _serviceProvider.GetService(typeof(AppDbContext)) as AppDbContext;
        db.Set<E>().Add(entity);
        db.SaveChanges();
    }

    // [DA-2] Business logic in repository - applying discount rules!
    public void AddOrderWithDiscount(Order order)
    {
        // [CLEAN-3] Business logic in infrastructure
        if (order.TotalAmount > 500)
            order.TotalAmount *= 0.85m;  // [ANTI-PATTERN: Magic number]
        else if (order.TotalAmount > 200)
            order.TotalAmount *= 0.9m;

        // [CLEAN-3] More business logic - status determination
        order.Status = order.TotalAmount > 1000 ? "priority" : "normal";

        var db = _serviceProvider.GetService(typeof(AppDbContext)) as AppDbContext;
        db.Orders.Add(order);
        db.SaveChanges();
    }
}
