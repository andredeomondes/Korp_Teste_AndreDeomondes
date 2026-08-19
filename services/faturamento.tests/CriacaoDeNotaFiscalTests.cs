using System.Net;
using System.Net.Http.Json;

namespace Faturamento.Tests;

public sealed class CriacaoDeNotaFiscalTests
    : IClassFixture<FaturamentoApiFixture>, IAsyncLifetime
{
    private readonly FaturamentoApiFixture _api;

    public CriacaoDeNotaFiscalTests(FaturamentoApiFixture api) => _api = api;

    public Task InitializeAsync() => _api.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Nota_fiscal_nasce_aberta_e_aparece_na_listagem_com_sua_numeracao()
    {
        var resposta = await _api.Client.PostAsync("/notas-fiscais", null);

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        var criada = await resposta.Content.ReadFromJsonAsync<NotaFiscalResponse>();
        Assert.Equal("Aberta", criada!.Status);
        Assert.True(criada.Numero > 0);

        var notas = await _api.Client.GetFromJsonAsync<NotaFiscalResponse[]>("/notas-fiscais");

        var nota = Assert.Single(notas!);
        Assert.Equal(criada.Numero, nota.Numero);
        Assert.Equal("Aberta", nota.Status);
    }

    [Fact]
    public async Task Location_da_nota_criada_leva_a_propria_nota()
    {
        var resposta = await _api.Client.PostAsync("/notas-fiscais", null);
        var criada = await resposta.Content.ReadFromJsonAsync<NotaFiscalResponse>();

        var location = resposta.Headers.Location;
        Assert.NotNull(location);

        var pelaLocation = await _api.Client.GetFromJsonAsync<NotaFiscalResponse>(location);

        Assert.Equal(criada!.Numero, pelaLocation!.Numero);
    }

    [Fact]
    public async Task Numeracao_cresce_e_nao_reusa_numero_de_nota_removida()
    {
        var primeira = await CriarNotaAsync();
        var segunda = await CriarNotaAsync();

        Assert.True(segunda.Numero > primeira.Numero);

        // Some com todas as Notas Fiscais e cria outra: a Numeração é um
        // contador global do sistema, então a próxima nota não pode receber um
        // número já usado.
        await _api.ResetAsync();

        var depoisDaLimpeza = await CriarNotaAsync();

        Assert.True(depoisDaLimpeza.Numero > segunda.Numero);
    }

    private async Task<NotaFiscalResponse> CriarNotaAsync()
    {
        var resposta = await _api.Client.PostAsync("/notas-fiscais", null);
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<NotaFiscalResponse>())!;
    }

    private sealed record NotaFiscalResponse(Guid Id, long Numero, string Status);
}
