using Faturamento.NotasFiscais;
using Microsoft.EntityFrameworkCore;
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

builder.Services.AddDbContext<FaturamentoDbContext>(options => options
    .UseNpgsql(builder.Configuration.GetConnectionString("Faturamento")
        ?? throw new InvalidOperationException("Connection string 'Faturamento' não configurada.")));

var app = builder.Build();

// O schema é aplicado na subida: o serviço é dono do próprio banco e nenhum
// passo manual deve ficar entre `docker compose up` e a API operante. Falhar em
// migrar não derruba o processo: o serviço precisa continuar de pé para que
// `/health` consiga reportar a indisponibilidade do banco em vez de sumir.
using (var scope = app.Services.CreateScope())
{
    try
    {
        await scope.ServiceProvider.GetRequiredService<FaturamentoDbContext>()
            .Database.MigrateAsync();
    }
    catch (NpgsqlException erro)
    {
        app.Logger.LogCritical(erro, "Falha ao aplicar as migrations do Faturamento.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(FrontendCors);

// O serviço só se considera saudável quando alcança o próprio banco: sem ele
// não há Nota Fiscal para ler nem gravar, então reportar "ok" seria mentira.
app.MapGet("/health", async (FaturamentoDbContext db, CancellationToken cancellationToken) =>
{
    try
    {
        await db.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
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

app.MapNotasFiscais();

app.Run();

record HealthStatus(string Service, string Status, string Database);

// Torna o host visível para os testes de integração, que sobem esta mesma
// aplicação via WebApplicationFactory<Program>.
public partial class Program;
