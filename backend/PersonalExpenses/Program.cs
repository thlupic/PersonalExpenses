using System.Text.Json;
using PersonalExpenses.Models;
using PersonalExpenses.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<GoogleSheetsOptions>(
    builder.Configuration.GetSection("GoogleSheets"));

builder.Services.AddSingleton<ILookupStorageService, GoogleSheetsLookupStorageService>();

var storageProvider = builder.Configuration["StorageProvider"];

if (string.Equals(storageProvider, "GoogleSheets", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<IExpenseStorageService, GoogleSheetsExpenseStorageService>();
}
else
{
    builder.Services.AddSingleton<IExpenseStorageService, JsonExpenseStorageService>();
}

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

        await context.Response.WriteAsJsonAsync(new
        {
            error = "An unexpected server error occurred."
        });
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");

app.MapGet("/api/expenses", async (
    IExpenseStorageService storageService,
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

app.MapGet("/api/expenses/{id:long}", async (long id, IExpenseStorageService storageService) =>
{
    var item = await storageService.GetByIdAsync(id);
    return item is null
        ? Results.NotFound(new { error = "Expense not found." })
        : Results.Ok(item);
});

app.MapPost("/api/expenses", async (ExpenseEntry entry, IExpenseStorageService storageService) =>
{
    var validationError = ValidateExpense(entry);
    if (validationError is not null)
        return Results.BadRequest(new { error = validationError });

    var saved = await storageService.AddAsync(entry);
    return Results.Created($"/api/expenses/{saved.Id}", saved);
});

app.MapPut("/api/expenses/{id:long}", async (long id, ExpenseEntry entry, IExpenseStorageService storageService) =>
{
    var validationError = ValidateExpense(entry);
    if (validationError is not null)
        return Results.BadRequest(new { error = validationError });

    var updated = await storageService.UpdateAsync(id, entry);
    return updated is null
        ? Results.NotFound(new { error = "Expense not found." })
        : Results.Ok(updated);
});

app.MapDelete("/api/expenses/{id:long}", async (long id, IExpenseStorageService storageService) =>
{
    var deleted = await storageService.DeleteAsync(id);
    return deleted
        ? Results.NoContent()
        : Results.NotFound(new { error = "Expense not found." });
});

app.MapGet("/api/lookups/descriptions", async (ILookupStorageService lookupStorageService) =>
    Results.Ok(await lookupStorageService.GetDescriptionsAsync()));

app.MapGet("/api/lookups/locations", async (ILookupStorageService lookupStorageService) =>
    Results.Ok(await lookupStorageService.GetLocationsAsync()));

app.MapGet("/api/lookups/expense-types", async (ILookupStorageService lookupStorageService) =>
    Results.Ok(await lookupStorageService.GetExpenseTypesAsync()));

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