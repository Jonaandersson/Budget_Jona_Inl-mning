
#nullable enable
using Budget_Jona_Inlämning.Data;
using Budget_Jona_Inlämning.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Budget_Jona_Inlämning.Services;


public class TransactionService : ITransactionService
{
    private readonly AppDbContext _context;

    public TransactionService(AppDbContext context)
    {
        this._context = context;
    }

    public async Task<List<Transaction>> GetAllAsync()
    {
        return await this._context.Transactions
            .Include(t => t.Category)
            .AsNoTracking()
            .OrderByDescending(t => t.Date)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<Transaction?> GetByIdAsync(int id)
    {
        return await this._context.Transactions
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == id)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Transaction transaction)
    {
        this._context.Transactions.Add(transaction);
        await this._context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task UpdateAsync(Transaction transaction)
    {
        this._context.Transactions.Update(transaction);
        await this._context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await this._context.Transactions.FindAsync(id).ConfigureAwait(false);
        if (entity is null)
        {
            return;
        }

        this._context.Transactions.Remove(entity);
        await this._context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<List<Transaction>> GetMonthlyAsync()
    {
        return await this._context.Transactions
            .Where(t => t.IsMonthly)
            .Include(t => t.Category)
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
    }
}

