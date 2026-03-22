namespace PersonalExpenses.Services;

public interface ILookupStorageService
{
    Task<List<string>> GetDescriptionsAsync();
    Task<List<string>> GetLocationsAsync();
    Task<List<string>> GetExpenseTypesAsync();
}