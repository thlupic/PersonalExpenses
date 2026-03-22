using System.Text.Json;
using PersonalExpenses.Models;
using PersonalExpenses.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<LookupService>();
builder.Services.AddSingleton<ExpenseStorageService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = "An unexpected server error occurred."
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");

app.MapGet("/api/lookups/descriptions", async (LookupService lookupService) =>
    Results.Ok(await lookupService.GetDescriptionsAsync()));

app.MapPost("/api/lookups/descriptions", async (LookupValueRequest request, LookupService lookupService) =>
{
    if (string.IsNullOrWhiteSpace(request.Value))
        return Results.BadRequest(new { error = "Value is required." });

    var items = await lookupService.AddDescriptionAsync(request.Value);
    return Results.Ok(items);
});

app.MapDelete("/api/lookups/descriptions/{value}", async (string value, LookupService lookupService) =>
{
    var deleted = await lookupService.DeleteDescriptionAsync(value);
    return deleted
        ? Results.NoContent()
        : Results.NotFound(new { error = "Description not found." });
});

app.MapGet("/api/lookups/locations", async (LookupService lookupService) =>
    Results.Ok(await lookupService.GetLocationsAsync()));

app.MapPost("/api/lookups/locations", async (LookupValueRequest request, LookupService lookupService) =>
{
    if (string.IsNullOrWhiteSpace(request.Value))
        return Results.BadRequest(new { error = "Value is required." });

    var items = await lookupService.AddLocationAsync(request.Value);
    return Results.Ok(items);
});

app.MapDelete("/api/lookups/locations/{value}", async (string value, LookupService lookupService) =>
{
    var deleted = await lookupService.DeleteLocationAsync(value);
    return deleted
        ? Results.NoContent()
        : Results.NotFound(new { error = "Location not found." });
});

app.MapGet("/api/lookups/expense-types", async (LookupService lookupService) =>
    Results.Ok(await lookupService.GetExpenseTypesAsync()));

app.MapPost("/api/lookups/expense-types", async (LookupValueRequest request, LookupService lookupService) =>
{
    if (string.IsNullOrWhiteSpace(request.Value))
        return Results.BadRequest(new { error = "Value is required." });

    var items = await lookupService.AddExpenseTypeAsync(request.Value);
    return Results.Ok(items);
});

app.MapDelete("/api/lookups/expense-types/{value}", async (string value, LookupService lookupService) =>
{
    var deleted = await lookupService.DeleteExpenseTypeAsync(value);
    return deleted
        ? Results.NoContent()
        : Results.NotFound(new { error = "Expense type not found." });
});

app.MapGet("/api/expenses", async (
    ExpenseStorageService storageService,
    string? expenseType,
    string? location,
    DateTime? dateFrom,
    DateTime? dateTo) =>
{
    var items = await storageService.GetAllAsync();

    if (!string.IsNullOrWhiteSpace(expenseType))
    {
        items = items
            .Where(x => string.Equals(x.ExpenseType, expenseType, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    if (!string.IsNullOrWhiteSpace(location))
    {
        items = items
            .Where(x => string.Equals(x.Location, location, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    if (dateFrom.HasValue)
    {
        items = items
            .Where(x => x.Date.Date >= dateFrom.Value.Date)
            .ToList();
    }

    if (dateTo.HasValue)
    {
        items = items
            .Where(x => x.Date.Date <= dateTo.Value.Date)
            .ToList();
    }

    var sorted = items
        .OrderByDescending(x => x.Date)
        .ThenByDescending(x => x.Id)
        .ToList();

    return Results.Ok(sorted);
});

app.MapGet("/api/expenses/{id:long}", async (long id, ExpenseStorageService storageService) =>
{
    var item = await storageService.GetByIdAsync(id);
    return item is null
        ? Results.NotFound(new { error = "Expense not found." })
        : Results.Ok(item);
});

app.MapPost("/api/expenses", async (ExpenseEntry entry, ExpenseStorageService storageService) =>
{
    var validationError = ValidateExpense(entry);
    if (validationError is not null)
        return Results.BadRequest(new { error = validationError });

    var saved = await storageService.AddAsync(entry);
    return Results.Created($"/api/expenses/{saved.Id}", saved);
});

app.MapPut("/api/expenses/{id:long}", async (long id, ExpenseEntry entry, ExpenseStorageService storageService) =>
{
    var validationError = ValidateExpense(entry);
    if (validationError is not null)
        return Results.BadRequest(new { error = validationError });

    var updated = await storageService.UpdateAsync(id, entry);
    return updated is null
        ? Results.NotFound(new { error = "Expense not found." })
        : Results.Ok(updated);
});

app.MapDelete("/api/expenses/{id:long}", async (long id, ExpenseStorageService storageService) =>
{
    var deleted = await storageService.DeleteAsync(id);
    return deleted
        ? Results.NoContent()
        : Results.NotFound(new { error = "Expense not found." });
});

app.Run();

static string? ValidateExpense(ExpenseEntry entry)
{
    if (string.IsNullOrWhiteSpace(entry.Description))
        return "Description is required.";

    if (string.IsNullOrWhiteSpace(entry.Location))
        return "Location is required.";

    if (string.IsNullOrWhiteSpace(entry.ExpenseType))
        return "Expense type is required.";

    if (entry.Quantity <= 0)
        return "Quantity must be greater than 0.";

    if (entry.Amount <= 0)
        return "Amount must be greater than 0.";

    return null;
}