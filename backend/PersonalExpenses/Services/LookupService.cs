using System.Text.Json;

namespace PersonalExpenses.Services;

public class LookupService
{
    private readonly string _dataFolder;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public LookupService(IWebHostEnvironment environment)
    {
        _dataFolder = Path.Combine(environment.ContentRootPath, "Data");
        Directory.CreateDirectory(_dataFolder);
    }

    public Task<List<string>> GetDescriptionsAsync()
    {
        return ReadStringListAsync("expenses.json");
    }

    public Task<List<string>> GetLocationsAsync()
    {
        return ReadStringListAsync("locations.json");
    }

    public Task<List<string>> GetExpenseTypesAsync()
    {
        return ReadStringListAsync("expense-types.json");
    }

    public Task<List<string>> AddDescriptionAsync(string value)
    {
        return AddValueAsync("expenses.json", value);
    }

    public Task<List<string>> AddLocationAsync(string value)
    {
        return AddValueAsync("locations.json", value);
    }

    public Task<List<string>> AddExpenseTypeAsync(string value)
    {
        return AddValueAsync("expense-types.json", value);
    }

    public Task<bool> DeleteDescriptionAsync(string value)
    {
        return DeleteValueAsync("expenses.json", value);
    }

    public Task<bool> DeleteLocationAsync(string value)
    {
        return DeleteValueAsync("locations.json", value);
    }

    public Task<bool> DeleteExpenseTypeAsync(string value)
    {
        return DeleteValueAsync("expense-types.json", value);
    }

    private async Task<List<string>> ReadStringListAsync(string fileName)
    {
        var filePath = Path.Combine(_dataFolder, fileName);

        if (!File.Exists(filePath))
        {
            await File.WriteAllTextAsync(filePath, "[]");
            return new List<string>();
        }

        var json = await File.ReadAllTextAsync(filePath);

        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }

        return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
    }

    private async Task<List<string>> AddValueAsync(string fileName, string value)
    {
        var normalizedValue = NormalizeValue(value);
        var items = await ReadStringListAsync(fileName);

        var exists = items.Any(x => string.Equals(x, normalizedValue, StringComparison.OrdinalIgnoreCase));
        if (!exists)
        {
            items.Add(normalizedValue);
            items = items
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            await WriteStringListAsync(fileName, items);
        }

        return items;
    }

    private async Task<bool> DeleteValueAsync(string fileName, string value)
    {
        var normalizedValue = NormalizeValue(value);
        var items = await ReadStringListAsync(fileName);

        var itemToRemove = items.FirstOrDefault(x =>
            string.Equals(x, normalizedValue, StringComparison.OrdinalIgnoreCase));

        if (itemToRemove is null)
        {
            return false;
        }

        items.Remove(itemToRemove);
        await WriteStringListAsync(fileName, items);

        return true;
    }

    private async Task WriteStringListAsync(string fileName, List<string> items)
    {
        var filePath = Path.Combine(_dataFolder, fileName);
        var json = JsonSerializer.Serialize(items, _jsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    private static string NormalizeValue(string value)
    {
        return value.Trim();
    }
}