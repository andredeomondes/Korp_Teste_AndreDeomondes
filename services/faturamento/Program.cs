using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

const string FrontendCors = "frontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCors, policy => policy
        .WithOrigins(builder.Configuration["Frontend:Origin"] ?? "http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.AddNpgsqlDataSource(
    builder.Configuration.GetConnectionString("Faturamento")
    ?? throw new InvalidOperationException("Connection string 'Faturamento' não configurada."));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(FrontendCors);

// O serviço só se considera saudável quando alcança o próprio banco: sem ele
// não há Nota Fiscal para ler nem gravar, então reportar "ok" seria mentira.
app.MapGet("/health", async (NpgsqlDataSource dataSource, CancellationToken cancellationToken) =>
{
    try
    {
        await using var command = dataSource.CreateCommand("SELECT 1");
        await command.ExecuteScalarAsync(cancellationToken);
        return Results.Ok(new HealthStatus("faturamento", "ok", "ok"));
    }
    catch (NpgsqlException)
    {
        return Results.Json(
            new HealthStatus("faturamento", "degraded", "unreachable"),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
})
.WithName("GetHealth");

app.Run();

record HealthStatus(string Service, string Status, string Database);
