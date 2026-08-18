using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Estoque.Tests;

/// <summary>
/// Sobe a API do Estoque contra um PostgreSQL real em contêiner. O banco é real
/// de propósito: as regras testadas aqui (Código único, Saldo não negativo) são
/// garantidas por restrições do schema, e um banco em memória não as executaria.
/// </summary>
public sealed class EstoqueApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .Build();

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        Client = CreateClient();
    }

    /// <summary>
    /// Cada teste começa com o Estoque vazio, para que um teste não enxergue os
    /// Produtos cadastrados por outro.
    /// </summary>
    public async Task ResetAsync()
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("TRUNCATE TABLE produtos", connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Derruba o PostgreSQL sob a API, para exercitar o comportamento do serviço
    /// quando perde o próprio banco.
    /// </summary>
    public Task PararBancoAsync() => _postgres.StopAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Estoque", _postgres.GetConnectionString());
    }

    public new async Task DisposeAsync()
    {
        Client?.Dispose();
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
