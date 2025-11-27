using Budget_Jona_Inlämning.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Budget_Jona_Inlämning.Services
{
    public interface ICategoryService
    {
        Task<List<Category>> GetAllAsync();
        Task<Category?> GetByIdAsync(int id);
        Task AddAsync(Category category);
        Task UpdateAsync(Category category);
        Task DeleteAsync(int id);
    }
}
