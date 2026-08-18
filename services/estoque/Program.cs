using Estoque.Produtos;
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

builder.Services.AddDbContext<EstoqueDbContext>(options => options
    .UseNpgsql(builder.Configuration.GetConnectionString("Estoque")
        ?? throw new InvalidOperationException("Connection string 'Estoque' não configurada.")));

var app = builder.Build();

// O schema é aplicado na subida: o serviço é dono do próprio banco e nenhum
// passo manual deve ficar entre `docker compose up` e a API operante. Falhar em
// migrar não derruba o processo: o serviço precisa continuar de pé para que
// `/health` consiga reportar a indisponibilidade do banco em vez de sumir.
using (var scope = app.Services.CreateScope())
{
    try
    {
        await scope.ServiceProvider.GetRequiredService<EstoqueDbContext>().Database.MigrateAsync();
    }
    catch (NpgsqlException erro)
    {
        app.Logger.LogCritical(erro, "Falha ao aplicar as migrations do Estoque.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(FrontendCors);

// O serviço só se considera saudável quando alcança o próprio banco: sem ele
// não há Saldo para consultar nem debitar, então reportar "ok" seria mentira.
app.MapGet("/health", async (EstoqueDbContext db, CancellationToken cancellationToken) =>
{
    try
    {
        await db.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
        return Results.Ok(new HealthStatus("estoque", "ok", "ok"));
    }
    catch (NpgsqlException)
    {
        return Results.Json(
            new HealthStatus("estoque", "degraded", "unreachable"),
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
})
.WithName("GetHealth");

app.MapProdutos();

app.Run();

record HealthStatus(string Service, string Status, string Database);

// Torna o host visível para os testes de integração, que sobem esta mesma
// aplicação via WebApplicationFactory<Program>.
public partial class Program;
