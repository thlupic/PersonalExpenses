using System.Text.Json;

namespace PersonalExpenses.Api.Services;

public class LookupService
{
    private readonly string _dataFolder;

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
}