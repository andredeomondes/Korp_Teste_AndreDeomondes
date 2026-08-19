using System.Net;

namespace Faturamento.Tests;

/// <summary>
/// Usa a própria instância de <see cref="FaturamentoApiFixture"/> porque derruba
/// o banco no meio do caminho — o que arruinaria os demais testes.
/// </summary>
public sealed class HealthTests : IClassFixture<FaturamentoApiFixture>
{
    private readonly FaturamentoApiFixture _api;

    public HealthTests(FaturamentoApiFixture api) => _api = api;

    [Fact]
    public async Task Health_reporta_indisponibilidade_quando_o_banco_cai_em_vez_de_derrubar_o_servico()
    {
        var comBanco = await _api.Client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, comBanco.StatusCode);

        await _api.PararBancoAsync();

        var semBanco = await _api.Client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, semBanco.StatusCode);
    }
}
