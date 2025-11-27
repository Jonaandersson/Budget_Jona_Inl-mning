#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using Budget_Jona_Inlämning.Models;

namespace Budget_Jona_Inlämning.Services;

public interface IIncomeLossService
{
    Task<List<IncomeLoss>> GetAllAsync();
    Task<IncomeLoss?> GetByIdAsync(int id);
    Task AddAsync(IncomeLoss item);
    Task UpdateAsync(IncomeLoss item);
    Task DeleteAsync(int id);
}