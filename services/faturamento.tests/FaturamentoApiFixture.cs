using Faturamento.Estoque;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Faturamento.Tests;

/// <summary>
/// Sobe a API do Faturamento contra um PostgreSQL real em contêiner. O banco é
/// real de propósito: a Numeração sequencial vem de uma sequence do PostgreSQL,
/// que nenhum banco em memória reproduz.
/// </summary>
public sealed class FaturamentoApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .Build();

    public HttpClient Client { get; private set; } = null!;

    /// <summary>O Estoque que este Faturamento enxerga durante os testes.</summary>
    public EstoqueFalso Estoque { get; } = new();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        Client = CreateClient();
    }

    /// <summary>
    /// Limpa as Notas Fiscais entre testes. A sequence da Numeração
    /// deliberadamente <em>não</em> é reiniciada: números não são reusados nem
    /// quando as notas somem, e os testes precisam enxergar isso.
    /// </summary>
    public async Task ResetAsync()
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();
        // itens_da_nota some junto por cascata da chave estrangeira.
        await using var command = new NpgsqlCommand(
            "TRUNCATE TABLE notas_fiscais CASCADE",
            connection);
        await command.ExecuteNonQueryAsync();

        Estoque.Limpar();
    }

    /// <summary>
    /// Derruba o PostgreSQL sob a API, para exercitar o comportamento do serviço
    /// quando perde o próprio banco.
    /// </summary>
    public Task PararBancoAsync() => _postgres.StopAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Faturamento", _postgres.GetConnectionString());
        builder.UseSetting("Servicos:Estoque", "http://estoque-de-teste");

        // Substitui apenas o transporte: o EstoqueHttpClient real continua no
        // caminho, então a montagem da URL e a leitura do JSON também são
        // exercitadas pelos testes.
        builder.ConfigureTestServices(services => services
            .AddHttpClient<IEstoqueClient, EstoqueHttpClient>(http =>
                http.BaseAddress = new Uri("http://estoque-de-teste"))
            .ConfigurePrimaryHttpMessageHandler(() => Estoque));
    }

    public new async Task DisposeAsync()
    {
        Client?.Dispose();
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
