using System.Net;
using System.Net.Http.Json;

namespace Faturamento.Tests;

/// <summary>
/// As invariantes do domínio vistas pela porta HTTP: nota Fechada é imutável, e
/// Impressão que deixaria algum Saldo negativo é recusada sem alterar nada.
/// </summary>
public sealed class RejeicoesDaImpressaoTests : IClassFixture<FaturamentoApiFixture>, IAsyncLifetime
{
    private readonly FaturamentoApiFixture _api;

    public RejeicoesDaImpressaoTests(FaturamentoApiFixture api) => _api = api;

    public Task InitializeAsync() => _api.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Recusa_do_estoque_mantem_a_nota_aberta_e_chega_ao_operador()
    {
        var cafe = _api.Estoque.Cadastrar("CAF-001", "Café em grãos 1kg", 1);
        var nota = await _api.Client.CriarNotaAsync((cafe, 5));

        _api.Estoque.RecusaProgramada =
            "O Produto CAF-001 tem Saldo 1 e a operação pediu 5.";

        var resposta = await _api.Client.PostAsync($"/notas-fiscais/{nota.Id}/impressao", null);

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);

        // O motivo do Estoque chega inteiro na resposta: é ele que a tela mostra,
        // e é ele que diz ao operador qual Produto faltou.
        Assert.Contains("CAF-001", await resposta.Content.ReadAsStringAsync());

        Assert.Equal("Aberta", (await _api.Client.ObterNotaAsync(nota.Id)).Status);
    }

    [Fact]
    public async Task Impressao_recusada_nao_confirma_debito_de_nenhum_item()
    {
        var cafe = _api.Estoque.Cadastrar("CAF-001", "Café em grãos 1kg", 10);
        var acucar = _api.Estoque.Cadastrar("ACU-001", "Açúcar refinado 1kg", 1);

        var nota = await _api.Client.CriarNotaAsync((cafe, 2), (acucar, 5));

        _api.Estoque.RecusaProgramada =
            "O Produto ACU-001 tem Saldo 1 e a operação pediu 5.";

        var resposta = await _api.Client.PostAsync($"/notas-fiscais/{nota.Id}/impressao", null);

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);

        // Nenhum débito ficou de pé — nem o do Café, que tinha Saldo de sobra e
        // vinha antes na lista. A atomicidade do débito em si é do Estoque, e
        // está testada lá; aqui o que importa é que o Faturamento não confirmou
        // nada e não fechou a nota.
        Assert.Empty(_api.Estoque.Debitos);
        Assert.Equal("Aberta", (await _api.Client.ObterNotaAsync(nota.Id)).Status);
    }

    [Fact]
    public async Task Nota_fechada_nao_aceita_novo_item()
    {
        var (nota, outroProduto) = await NotaFechadaAsync();

        var resposta = await _api.Client.PostAsJsonAsync(
            $"/notas-fiscais/{nota.Id}/itens",
            new { produtoId = outroProduto, quantidade = 1 });

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Single((await _api.Client.ObterNotaAsync(nota.Id)).Itens);
    }

    [Fact]
    public async Task Nota_fechada_nao_aceita_alteracao_de_quantidade()
    {
        var (nota, _) = await NotaFechadaAsync();
        var item = (await _api.Client.ObterNotaAsync(nota.Id)).Itens.Single();

        var resposta = await _api.Client.PutAsJsonAsync(
            $"/notas-fiscais/{nota.Id}/itens/{item.Id}",
            new { quantidade = 99 });

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Equal(2, (await _api.Client.ObterNotaAsync(nota.Id)).Itens.Single().Quantidade);
    }

    [Fact]
    public async Task Nota_fechada_nao_aceita_remocao_de_item()
    {
        var (nota, _) = await NotaFechadaAsync();
        var item = (await _api.Client.ObterNotaAsync(nota.Id)).Itens.Single();

        var resposta = await _api.Client.DeleteAsync(
            $"/notas-fiscais/{nota.Id}/itens/{item.Id}");

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Single((await _api.Client.ObterNotaAsync(nota.Id)).Itens);
    }

    /// <summary>Uma Nota Fiscal já impressa, e um Produto que ficou de fora dela.</summary>
    private async Task<(NotaFiscalResponse Nota, Guid OutroProduto)> NotaFechadaAsync()
    {
        var cafe = _api.Estoque.Cadastrar("CAF-001", "Café em grãos 1kg", 10);
        var acucar = _api.Estoque.Cadastrar("ACU-001", "Açúcar refinado 1kg", 20);

        var nota = await _api.Client.CriarNotaAsync((cafe, 2));

        var impressao = await _api.Client.PostAsync($"/notas-fiscais/{nota.Id}/impressao", null);
        impressao.EnsureSuccessStatusCode();

        return (nota, acucar);
    }

}
