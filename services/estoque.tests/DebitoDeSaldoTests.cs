using System.Net;
using System.Net.Http.Json;

namespace Estoque.Tests;

public sealed class DebitoDeSaldoTests : IClassFixture<EstoqueApiFixture>, IAsyncLifetime
{
    private readonly EstoqueApiFixture _api;

    public DebitoDeSaldoTests(EstoqueApiFixture api) => _api = api;

    public Task InitializeAsync() => _api.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Debito_reduz_o_saldo_de_cada_produto_pela_quantidade_usada()
    {
        // Exemplo do enunciado: Saldo 10, nota usa 2 unidades, novo Saldo 8.
        var cafe = await CadastrarAsync("CAF-001", "Café em grãos 1kg", 10);
        var acucar = await CadastrarAsync("ACU-001", "Açúcar refinado 1kg", 20);

        var resposta = await _api.Client.PostAsJsonAsync(
            "/debitos",
            new
            {
                itens = new[]
                {
                    new { produtoId = cafe.Id, quantidade = 2 },
                    new { produtoId = acucar.Id, quantidade = 5 },
                },
            });

        Assert.True(
            resposta.StatusCode == HttpStatusCode.NoContent,
            await resposta.Content.ReadAsStringAsync());

        Assert.Equal(8, await SaldoAsync(cafe.Id));
        Assert.Equal(15, await SaldoAsync(acucar.Id));
    }

    [Fact]
    public async Task Debito_que_falha_em_um_item_nao_altera_o_saldo_de_nenhum()
    {
        var cafe = await CadastrarAsync("CAF-001", "Café em grãos 1kg", 10);
        var acucar = await CadastrarAsync("ACU-001", "Açúcar refinado 1kg", 1);

        var resposta = await _api.Client.PostAsJsonAsync(
            "/debitos",
            new
            {
                itens = new[]
                {
                    new { produtoId = cafe.Id, quantidade = 2 },
                    new { produtoId = acucar.Id, quantidade = 5 },
                },
            });

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);

        // A recusa diz qual Produto ficou sem Saldo: é essa frase que o
        // Faturamento repassa e o operador lê na tela.
        Assert.Contains("ACU-001", await resposta.Content.ReadAsStringAsync());

        // O Café tinha Saldo de sobra e vinha antes na lista: se o débito não
        // fosse atômico, ele teria sido reduzido antes de o Açúcar falhar.
        Assert.Equal(10, await SaldoAsync(cafe.Id));
        Assert.Equal(1, await SaldoAsync(acucar.Id));
    }

    [Fact]
    public async Task Quantidade_nao_positiva_e_recusada_como_erro_de_validacao()
    {
        var cafe = await CadastrarAsync("CAF-001", "Café em grãos 1kg", 10);

        var resposta = await _api.Client.PostAsJsonAsync(
            "/debitos",
            new { itens = new[] { new { produtoId = cafe.Id, quantidade = -5 } } });

        // 400 e não 409: o problema é a requisição, não o Saldo do Produto.
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Equal(10, await SaldoAsync(cafe.Id));
    }

    [Fact]
    public async Task Produto_inexistente_no_debito_nao_altera_saldo_de_ninguem()
    {
        var cafe = await CadastrarAsync("CAF-001", "Café em grãos 1kg", 10);

        var resposta = await _api.Client.PostAsJsonAsync(
            "/debitos",
            new
            {
                itens = new[]
                {
                    new { produtoId = cafe.Id, quantidade = 2 },
                    new { produtoId = Guid.CreateVersion7(), quantidade = 1 },
                },
            });

        Assert.Equal(HttpStatusCode.UnprocessableContent, resposta.StatusCode);
        Assert.Equal(10, await SaldoAsync(cafe.Id));
    }

    private async Task<ProdutoResponse> CadastrarAsync(string codigo, string descricao, int saldo)
    {
        var resposta = await _api.Client.PostAsJsonAsync(
            "/produtos",
            new { codigo, descricao, saldo });

        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<ProdutoResponse>())!;
    }

    private async Task<int> SaldoAsync(Guid id) =>
        (await _api.Client.GetFromJsonAsync<ProdutoResponse>($"/produtos/{id}"))!.Saldo;

    private sealed record ProdutoResponse(Guid Id, string Codigo, string Descricao, int Saldo);
}
