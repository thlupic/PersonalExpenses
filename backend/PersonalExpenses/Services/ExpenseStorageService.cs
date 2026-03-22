using System.Text.Json;
using PersonalExpenses.Models;

namespace PersonalExpenses.Services;

public class ExpenseStorageService
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public ExpenseStorageService(IWebHostEnvironment environment)
    {
        var dataFolder = Path.Combine(environment.ContentRootPath, "Data");
        Directory.CreateDirectory(dataFolder);

        _filePath = Path.Combine(dataFolder, "expense-entries.json");

        if (!File.Exists(_filePath))
        {
            File.WriteAllText(_filePath, "[]");
        }
    }

    public async Task<List<ExpenseEntry>> GetAllAsync()
    {
        var json = await File.ReadAllTextAsync(_filePath);

        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<ExpenseEntry>();
        }

        return JsonSerializer.Deserialize<List<ExpenseEntry>>(json) ?? new List<ExpenseEntry>();
    }

    public async Task<ExpenseEntry?> GetByIdAsync(long id)
    {
        var items = await GetAllAsync();
        return items.FirstOrDefault(x => x.Id == id);
    }

    public async Task<ExpenseEntry> AddAsync(ExpenseEntry entry)
    {
        var items = await GetAllAsync();

        entry.Id = items.Count == 0 ? 1 : items.Max(x => x.Id) + 1;
        items.Add(entry);

        await SaveAllAsync(items);

        return entry;
    }

    public async Task<ExpenseEntry?> UpdateAsync(long id, ExpenseEntry updatedEntry)
    {
        var items = await GetAllAsync();

        var existing = items.FirstOrDefault(x => x.Id == id);
        if (existing == null)
        {
            return null;
        }

        existing.Date = updatedEntry.Date;
        existing.Description = updatedEntry.Description;
        existing.Location = updatedEntry.Location;
        existing.Quantity = updatedEntry.Quantity;
        existing.ExpenseType = updatedEntry.ExpenseType;
        existing.Amount = updatedEntry.Amount;

        await SaveAllAsync(items);

        return existing;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var items = await GetAllAsync();

        var existing = items.FirstOrDefault(x => x.Id == id);
        if (existing == null)
        {
            return false;
        }

        items.Remove(existing);
        await SaveAllAsync(items);

        return true;
    }

    public async Task SaveAllAsync(List<ExpenseEntry> items)
    {
        var json = JsonSerializer.Serialize(items, _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }
}