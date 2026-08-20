using System.Net;
using System.Net.Http.Json;

namespace Faturamento.Tests;

public sealed class ImpressaoTests : IClassFixture<FaturamentoApiFixture>, IAsyncLifetime
{
    private readonly FaturamentoApiFixture _api;

    public ImpressaoTests(FaturamentoApiFixture api) => _api = api;

    public Task InitializeAsync() => _api.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Imprimir_fecha_a_nota_e_debita_o_saldo_de_cada_item()
    {
        var cafe = _api.Estoque.Cadastrar("CAF-001", "Café em grãos 1kg", 10);
        var acucar = _api.Estoque.Cadastrar("ACU-001", "Açúcar refinado 1kg", 20);

        var nota = await CriarNotaComItensAsync((cafe, 2), (acucar, 5));

        var resposta = await _api.Client.PostAsync($"/notas-fiscais/{nota.Id}/impressao", null);

        Assert.True(
            resposta.StatusCode == HttpStatusCode.OK,
            await resposta.Content.ReadAsStringAsync());

        var impressa = await resposta.Content.ReadFromJsonAsync<NotaFiscalResponse>();
        Assert.Equal("Fechada", impressa!.Status);

        // O Estoque recebeu a baixa de cada Item da Nota, com a quantidade usada.
        Assert.Equal(2, _api.Estoque.Debitos.Count);
        Assert.Equal(2, _api.Estoque.Debitos.Single(d => d.ProdutoId == cafe).Quantidade);
        Assert.Equal(5, _api.Estoque.Debitos.Single(d => d.ProdutoId == acucar).Quantidade);
    }

    [Fact]
    public async Task Nota_impressa_continua_fechada_na_listagem()
    {
        var cafe = _api.Estoque.Cadastrar("CAF-001", "Café em grãos 1kg", 10);
        var nota = await CriarNotaComItensAsync((cafe, 2));

        await _api.Client.PostAsync($"/notas-fiscais/{nota.Id}/impressao", null);

        var detalhe = await _api.Client.GetFromJsonAsync<NotaFiscalResponse>(
            $"/notas-fiscais/{nota.Id}");

        Assert.Equal("Fechada", detalhe!.Status);
    }

    [Fact]
    public async Task Nota_ja_fechada_nao_e_impressa_de_novo_nem_debita_outra_vez()
    {
        var cafe = _api.Estoque.Cadastrar("CAF-001", "Café em grãos 1kg", 10);
        var nota = await CriarNotaComItensAsync((cafe, 2));

        await _api.Client.PostAsync($"/notas-fiscais/{nota.Id}/impressao", null);
        var segunda = await _api.Client.PostAsync($"/notas-fiscais/{nota.Id}/impressao", null);

        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);

        // O Estoque foi chamado uma vez só: a segunda tentativa nem chegou nele.
        Assert.Single(_api.Estoque.Debitos);
    }

    [Fact]
    public async Task Nota_sem_itens_nao_vai_ao_estoque()
    {
        var nota = await CriarNotaComItensAsync();

        var resposta = await _api.Client.PostAsync($"/notas-fiscais/{nota.Id}/impressao", null);

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Empty(_api.Estoque.Debitos);
    }

    [Fact]
    public async Task Estoque_fora_do_ar_deixa_a_nota_aberta()
    {
        var cafe = _api.Estoque.Cadastrar("CAF-001", "Café em grãos 1kg", 10);
        var nota = await CriarNotaComItensAsync((cafe, 2));

        _api.Estoque.ForaDoAr = true;

        var resposta = await _api.Client.PostAsync($"/notas-fiscais/{nota.Id}/impressao", null);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, resposta.StatusCode);

        _api.Estoque.ForaDoAr = false;
        var detalhe = await _api.Client.GetFromJsonAsync<NotaFiscalResponse>(
            $"/notas-fiscais/{nota.Id}");

        Assert.Equal("Aberta", detalhe!.Status);
    }

    private async Task<NotaFiscalResponse> CriarNotaComItensAsync(
        params (Guid ProdutoId, int Quantidade)[] itens)
    {
        var criada = await _api.Client.PostAsync("/notas-fiscais", null);
        var nota = (await criada.Content.ReadFromJsonAsync<NotaFiscalResponse>())!;

        foreach (var (produtoId, quantidade) in itens)
        {
            var resposta = await _api.Client.PostAsJsonAsync(
                $"/notas-fiscais/{nota.Id}/itens",
                new { produtoId, quantidade });

            resposta.EnsureSuccessStatusCode();
        }

        return nota;
    }

    private sealed record NotaFiscalResponse(Guid Id, long Numero, string Status);
}
