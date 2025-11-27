#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Budget_Jona_Inlämning.Data;
using Budget_Jona_Inlämning.Models;

namespace Budget_Jona_Inlämning.Services;

public class IncomeLossService : IIncomeLossService
{
    private readonly AppDbContext _context;

    public IncomeLossService(AppDbContext context)
    {
        this._context = context;
    }

    public async Task<List<IncomeLoss>> GetAllAsync()
    {
        return await this._context.IncomeLosses
            .AsNoTracking()
            .OrderByDescending(i => i.Date)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<IncomeLoss?> GetByIdAsync(int id)
    {
        return await this._context.IncomeLosses.FindAsync(id).ConfigureAwait(false);
    }

    public async Task AddAsync(IncomeLoss item)
    {
        this._context.IncomeLosses.Add(item);
        await this._context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task UpdateAsync(IncomeLoss item)
    {
        this._context.IncomeLosses.Update(item);
        await this._context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await this._context.IncomeLosses.FindAsync(id).ConfigureAwait(false);
        if (entity is null)
        {
            return;
        }

        this._context.IncomeLosses.Remove(entity);
        await this._context.SaveChangesAsync().ConfigureAwait(false);
    }
}