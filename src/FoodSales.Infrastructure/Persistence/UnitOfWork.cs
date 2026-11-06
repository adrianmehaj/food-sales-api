using System.Data;
using FoodSales.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FoodSales.Infrastructure.Persistence;

public sealed class UnitOfWork(ShopDbContext db) : IUnitOfWork
{
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct)
    {
        // Serializable and rowversion protect stock, credit balances and retries across simultaneous requests.
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var result = await action();
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return result;
    }
}
