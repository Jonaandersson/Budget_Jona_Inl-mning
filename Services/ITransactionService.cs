#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using Budget_Jona_Inlämning.Models;

namespace Budget_Jona_Inlämning.Services;

public interface ITransactionService
{
    Task<List<Transaction>> GetAllAsync();
    Task<Transaction?> GetByIdAsync(int id);
    Task AddAsync(Transaction transaction);
    Task UpdateAsync(Transaction transaction);
    Task DeleteAsync(int id);
    Task<List<Transaction>> GetMonthlyAsync();
}