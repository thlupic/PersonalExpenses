using System.Text.Json;
using PersonalExpenses.Models;

namespace PersonalExpenses.Services;

public class ExpenseStorageService
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

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
        return string.IsNullOrWhiteSpace(json)
            ? new List<ExpenseEntry>()
            : JsonSerializer.Deserialize<List<ExpenseEntry>>(json) ?? new List<ExpenseEntry>();
    }

    public async Task<ExpenseEntry> AddAsync(ExpenseEntry entry)
    {
        var items = await GetAllAsync();
        entry.Id = items.Count == 0 ? 1 : items.Max(x => x.Id) + 1;
        items.Add(entry);

        var json = JsonSerializer.Serialize(items, _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json);

        return entry;
    }
}