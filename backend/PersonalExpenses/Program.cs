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
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");

app.MapGet("/api/lookups/descriptions", async (LookupService lookupService) =>
    Results.Ok(await lookupService.GetDescriptionsAsync()));

app.MapGet("/api/lookups/locations", async (LookupService lookupService) =>
    Results.Ok(await lookupService.GetLocationsAsync()));

app.MapGet("/api/lookups/expense-types", async (LookupService lookupService) =>
    Results.Ok(await lookupService.GetExpenseTypesAsync()));

app.MapGet("/api/expenses", async (ExpenseStorageService storageService) =>
{
    var items = await storageService.GetAllAsync();
    var sorted = items
        .OrderByDescending(x => x.Date)
        .ThenByDescending(x => x.Id);

    return Results.Ok(sorted);
});

app.MapPost("/api/expenses", async (ExpenseEntry entry, ExpenseStorageService storageService) =>
{
    if (string.IsNullOrWhiteSpace(entry.Description))
        return Results.BadRequest("Description is required.");

    if (string.IsNullOrWhiteSpace(entry.Location))
        return Results.BadRequest("Location is required.");

    if (string.IsNullOrWhiteSpace(entry.ExpenseType))
        return Results.BadRequest("ExpenseType is required.");

    if (entry.Quantity <= 0)
        return Results.BadRequest("Quantity must be greater than 0.");

    if (entry.Amount <= 0)
        return Results.BadRequest("Amount must be greater than 0.");

    var saved = await storageService.AddAsync(entry);
    return Results.Created($"/api/expenses/{saved.Id}", saved);
});

app.Run();