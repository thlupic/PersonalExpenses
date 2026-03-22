namespace PersonalExpenses.Api.Models;

public class ExpenseEntry
{
    public long Id { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string ExpenseType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}