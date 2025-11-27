#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Budget_Jona_Inlämning.Data;
using Budget_Jona_Inlämning.Models;

namespace Budget_Jona_Inlämning.Services;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _context;

    public CategoryService(AppDbContext context)
    {
        this._context = context;
    }

    public async Task<List<Category>> GetAllAsync()
    {
        // return detached entities to avoid tracking conflicts in the UI layer
        return await this._context.Categories
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<Category?> GetByIdAsync(int id)
    {
        // return a detached entity so callers get a copy they can edit safely
        return await this._context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id)
            .ConfigureAwait(false);
    }

    public async Task AddAsync(Category category)
    {
        this._context.Categories.Add(category);
        await this._context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task UpdateAsync(Category category)
    {
        // Fetch the tracked entity from the context and apply changes to it.
        var tracked = await this._context.Categories.FindAsync(category.Id).ConfigureAwait(false);
        if (tracked is null)
        {
            // If not found in the DB, you might want to throw or add it
            // Here we add it as a new entity
            this._context.Categories.Add(category);
        }
        else
        {
            // Apply only the properties you intend to update
            tracked.Name = category.Name;
            // If you add more properties later, copy them here
            this._context.Categories.Update(tracked);
        }

        await this._context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await this._context.Categories.FindAsync(id).ConfigureAwait(false);
        if (entity is null)
        {
            return;
        }

        this._context.Categories.Remove(entity);
        await this._context.SaveChangesAsync().ConfigureAwait(false);
    }
}