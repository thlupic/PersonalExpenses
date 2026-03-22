using PersonalExpenses.Models;

namespace PersonalExpenses.Services;

public interface IExpenseStorageService
{
    Task<List<ExpenseEntry>> GetAllAsync();
    Task<ExpenseEntry?> GetByIdAsync(long id);
    Task<ExpenseEntry> AddAsync(ExpenseEntry entry);
    Task<ExpenseEntry?> UpdateAsync(long id, ExpenseEntry updatedEntry);
    Task<bool> DeleteAsync(long id);
}