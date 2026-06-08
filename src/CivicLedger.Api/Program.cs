using System.Text.Json.Serialization;
using CivicLedger.Application;
using CivicLedger.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddDbContext<CivicLedgerDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("CivicLedger")));
builder.Services.AddScoped<IGrantRepository, GrantRepository>();
builder.Services.AddScoped<GrantService>();
builder.Services.AddSingleton<RiskAssessmentService>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "healthy",
    service = "CivicLedger API",
    timestampUtc = DateTime.UtcNow
}));

var grants = app.MapGroup("/api/grants").WithTags("Grants");

grants.MapGet("/", async (GrantService service, CancellationToken cancellationToken) =>
    Results.Ok(await service.GetAllAsync(cancellationToken)));

grants.MapGet("/{id:guid}", async (
    Guid id,
    GrantService service,
    CancellationToken cancellationToken) =>
{
    var grant = await service.GetByIdAsync(id, cancellationToken);
    return grant is null ? Results.NotFound() : Results.Ok(grant);
});

grants.MapPost("/", async (
    CreateGrantRequest request,
    GrantService service,
    CancellationToken cancellationToken) =>
{
    try
    {
        var grant = await service.CreateAsync(request, cancellationToken);
        return Results.Created($"/api/grants/{grant.Id}", grant);
    }
    catch (ArgumentException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["grant"] = [exception.Message]
        });
    }
});

grants.MapPost("/{id:guid}/activate", async (
    Guid id,
    GrantService service,
    CancellationToken cancellationToken) =>
{
    try
    {
        var grant = await service.ActivateAsync(id, cancellationToken);
        return grant is null ? Results.NotFound() : Results.Ok(grant);
    }
    catch (InvalidOperationException exception)
    {
        return Results.Conflict(new { error = exception.Message });
    }
});

grants.MapPost("/{id:guid}/expenses", async (
    Guid id,
    AddExpenseRequest request,
    GrantService service,
    CancellationToken cancellationToken) =>
{
    try
    {
        var grant = await service.AddExpenseAsync(id, request, cancellationToken);
        return grant is null ? Results.NotFound() : Results.Ok(grant);
    }
    catch (ArgumentException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["expense"] = [exception.Message]
        });
    }
    catch (InvalidOperationException exception)
    {
        return Results.Conflict(new { error = exception.Message });
    }
});

grants.MapGet("/{id:guid}/risk", async (
    Guid id,
    DateOnly? asOf,
    GrantService service,
    CancellationToken cancellationToken) =>
{
    var assessment = await service.AssessRiskAsync(
        id,
        asOf ?? DateOnly.FromDateTime(DateTime.UtcNow),
        cancellationToken);
    return assessment is null ? Results.NotFound() : Results.Ok(assessment);
});

app.MapGet("/api/dashboard", async (
    GrantService service,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.GetDashboardAsync(cancellationToken)))
    .WithTags("Dashboard");

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CivicLedgerDbContext>();
    await DatabaseSeeder.SeedAsync(dbContext);
}

app.MapFallbackToFile("index.html");
app.Run();

public partial class Program;
