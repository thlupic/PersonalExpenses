namespace PersonalExpenses.Models;

public class GoogleSheetsOptions
{
    public string SpreadsheetId { get; set; } = string.Empty;
    public string WorksheetName { get; set; } = "Expenses";
    public string CredentialsFilePath { get; set; } = string.Empty;
}