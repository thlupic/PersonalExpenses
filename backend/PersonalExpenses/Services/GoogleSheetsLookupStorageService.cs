using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Microsoft.Extensions.Options;
using PersonalExpenses.Models;

namespace PersonalExpenses.Services;

public class GoogleSheetsLookupStorageService : ILookupStorageService
{
    private readonly GoogleSheetsOptions _options;
    private readonly SheetsService _sheetsService;

    public GoogleSheetsLookupStorageService(IOptions<GoogleSheetsOptions> options)
    {
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.SpreadsheetId))
            throw new InvalidOperationException("GoogleSheets:SpreadsheetId is not configured.");

        if (string.IsNullOrWhiteSpace(_options.CredentialsFilePath))
            throw new InvalidOperationException("GoogleSheets:CredentialsFilePath is not configured.");

        if (!File.Exists(_options.CredentialsFilePath))
            throw new FileNotFoundException("Google service account credentials file not found.", _options.CredentialsFilePath);

        GoogleCredential credential;
        using (var stream = new FileStream(_options.CredentialsFilePath, FileMode.Open, FileAccess.Read))
        {
            credential = GoogleCredential.FromStream(stream)
                .CreateScoped(SheetsService.Scope.SpreadsheetsReadonly);
        }

        _sheetsService = new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "PersonalExpenses"
        });
    }

    public Task<List<string>> GetDescriptionsAsync()
    {
        return ReadSingleColumnAsync("Descriptions");
    }

    public Task<List<string>> GetLocationsAsync()
    {
        return ReadSingleColumnAsync("Locations");
    }

    public Task<List<string>> GetExpenseTypesAsync()
    {
        return ReadSingleColumnAsync("ExpenseTypes");
    }

    private async Task<List<string>> ReadSingleColumnAsync(string worksheetName)
    {
        var range = $"{worksheetName}!A2:A";
        var request = _sheetsService.Spreadsheets.Values.Get(_options.SpreadsheetId, range);
        var response = await request.ExecuteAsync();

        var result = new List<string>();

        if (response.Values == null || response.Values.Count == 0)
        {
            return result;
        }

        foreach (var row in response.Values)
        {
            if (row.Count == 0)
                continue;

            var value = row[0]?.ToString()?.Trim();
            if (!string.IsNullOrWhiteSpace(value))
            {
                result.Add(value);
            }
        }

        return result
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}