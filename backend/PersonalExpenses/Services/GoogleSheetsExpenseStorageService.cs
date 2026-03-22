using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.Extensions.Options;
using PersonalExpenses.Models;
using System.Globalization;

namespace PersonalExpenses.Services;

public class GoogleSheetsExpenseStorageService : IExpenseStorageService
{
    private readonly GoogleSheetsOptions _options;
    private readonly SheetsService _sheetsService;

    public GoogleSheetsExpenseStorageService(IOptions<GoogleSheetsOptions> options)
    {
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.SpreadsheetId))
            throw new InvalidOperationException("GoogleSheets:SpreadsheetId is not configured.");

        if (string.IsNullOrWhiteSpace(_options.WorksheetName))
            throw new InvalidOperationException("GoogleSheets:WorksheetName is not configured.");

        if (string.IsNullOrWhiteSpace(_options.CredentialsFilePath))
            throw new InvalidOperationException("GoogleSheets:CredentialsFilePath is not configured.");

        if (!File.Exists(_options.CredentialsFilePath))
            throw new FileNotFoundException("Google service account credentials file not found.", _options.CredentialsFilePath);

        GoogleCredential credential;
        using (var stream = new FileStream(_options.CredentialsFilePath, FileMode.Open, FileAccess.Read))
        {
            credential = GoogleCredential.FromStream(stream)
                .CreateScoped(SheetsService.Scope.Spreadsheets);
        }

        _sheetsService = new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "PersonalExpenses"
        });
    }

    public async Task<List<ExpenseEntry>> GetAllAsync()
    {
        var range = $"{_options.WorksheetName}!A2:G";
        var request = _sheetsService.Spreadsheets.Values.Get(_options.SpreadsheetId, range);
        var response = await request.ExecuteAsync();

        var items = new List<ExpenseEntry>();

        if (response.Values == null || response.Values.Count == 0)
        {
            return items;
        }

        foreach (var row in response.Values)
        {
            if (row.Count == 0 || string.IsNullOrWhiteSpace(row[0]?.ToString()))
            {
                continue;
            }

            items.Add(MapRowToExpense(row));
        }

        return items;
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

        await WriteAllAsync(items);

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

        await WriteAllAsync(items);

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
        await WriteAllAsync(items);

        return true;
    }

    private ExpenseEntry MapRowToExpense(IList<object> row)
    {
        static string GetCell(IList<object> cells, int index)
        {
            return index < cells.Count ? cells[index]?.ToString()?.Trim() ?? string.Empty : string.Empty;
        }

        var idText = GetCell(row, 0);
        var dateText = GetCell(row, 1);
        var description = GetCell(row, 2);
        var location = GetCell(row, 3);
        var quantityText = GetCell(row, 4);
        var expenseType = GetCell(row, 5);
        var amountText = GetCell(row, 6);

        return new ExpenseEntry
        {
            Id = long.TryParse(idText, out var id) ? id : 0,
            Date = DateTime.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date)
                ? date
                : DateTime.MinValue,
            Description = description,
            Location = location,
            Quantity = decimal.TryParse(quantityText, NumberStyles.Any, CultureInfo.InvariantCulture, out var quantity)
                ? quantity
                : 0,
            ExpenseType = expenseType,
            Amount = decimal.TryParse(amountText, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount)
                ? amount
                : 0
        };
    }

    private async Task WriteAllAsync(List<ExpenseEntry> items)
    {
        await EnsureHeaderRowAsync();

        var clearRange = $"{_options.WorksheetName}!A2:G";
        var clearRequest = new ClearValuesRequest();
        await _sheetsService.Spreadsheets.Values.Clear(clearRequest, _options.SpreadsheetId, clearRange).ExecuteAsync();

        if (items.Count == 0)
        {
            return;
        }

        var values = items
            .OrderBy(x => x.Id)
            .Select(x => (IList<object>)new List<object>
            {
                x.Id.ToString(CultureInfo.InvariantCulture),
                x.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                x.Description,
                x.Location,
                x.Quantity.ToString(CultureInfo.InvariantCulture),
                x.ExpenseType,
                x.Amount.ToString(CultureInfo.InvariantCulture)
            })
            .ToList();

        var body = new ValueRange
        {
            Values = values
        };

        var writeRange = $"{_options.WorksheetName}!A2:G";
        var updateRequest = _sheetsService.Spreadsheets.Values.Update(body, _options.SpreadsheetId, writeRange);
        updateRequest.ValueInputOption =
            SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;

        await updateRequest.ExecuteAsync();
    }

    private async Task EnsureHeaderRowAsync()
    {
        var headerValues = new List<IList<object>>
        {
            new List<object>
            {
                "Id",
                "Date",
                "Description",
                "Location",
                "Quantity",
                "ExpenseType",
                "Amount"
            }
        };

        var body = new ValueRange
        {
            Values = headerValues
        };

        var headerRange = $"{_options.WorksheetName}!A1:G1";
        var updateRequest = _sheetsService.Spreadsheets.Values.Update(body, _options.SpreadsheetId, headerRange);
        updateRequest.ValueInputOption =
            SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;

        await updateRequest.ExecuteAsync();
    }
}