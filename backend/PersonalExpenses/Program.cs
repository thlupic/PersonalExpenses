using PersonalExpenses.Models;
using PersonalExpenses.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<LookupService>();
builder.Services.AddSingleton<ExpenseStorageService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("Frontend");

app.MapGet("/api/lookups/descriptions", async (LookupService lookupService) =>
{
    var items = await lookupService.GetDescriptionsAsync();
    return Results.Ok(items);
});

app.MapGet("/api/lookups/locations", async (LookupService lookupService) =>
{
    var items = await lookupService.GetLocationsAsync();
    return Results.Ok(items);
});

app.MapGet("/api/lookups/expense-types", async (LookupService lookupService) =>
{
    var items = await lookupService.GetExpenseTypesAsync();
    return Results.Ok(items);
});

app.MapGet("/api/expenses", async (ExpenseStorageService storageService) =>
{
    var items = await storageService.GetAllAsync();
    return Results.Ok(items);
});

app.MapPost("/api/expenses", async (ExpenseEntry entry, ExpenseStorageService storageService) =>
{
    if (string.IsNullOrWhiteSpace(entry.Description))
    {
        return Results.BadRequest("Description is required.");
    }

    if (string.IsNullOrWhiteSpace(entry.Location))
    {
        return Results.BadRequest("Location is required.");
    }

    if (string.IsNullOrWhiteSpace(entry.ExpenseType))
    {
        return Results.BadRequest("ExpenseType is required.");
    }

    if (entry.Quantity < 0)
    {
        return Results.BadRequest("Quantity must be 0 or greater.");
    }

    if (entry.Amount < 0)
    {
        return Results.BadRequest("Amount must be 0 or greater.");
    }

    var saved = await storageService.AddAsync(entry);
    return Results.Created($"/api/expenses/{saved.Id}", saved);
});

app.Run();