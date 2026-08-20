using System.Net;
using System.Net.Http.Json;

namespace Faturamento.Tests;

public sealed class ItensDaNotaTests : IClassFixture<FaturamentoApiFixture>, IAsyncLifetime
{
    private readonly FaturamentoApiFixture _api;

    public ItensDaNotaTests(FaturamentoApiFixture api) => _api = api;

    public Task InitializeAsync() => _api.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Produto_adicionado_aparece_nos_itens_da_nota_com_codigo_e_descricao()
    {
        var produtoId = _api.Estoque.Cadastrar("CAF-001", "Café em grãos 1kg", 10);
        var nota = await _api.Client.CriarNotaAsync();

        var resposta = await _api.Client.PostAsJsonAsync(
            $"/notas-fiscais/{nota.Id}/itens",
            new { produtoId, quantidade = 3 });

        // O corpo entra na asserção para que uma falha inesperada apareça na
        // mensagem do teste em vez de virar um "esperava 201, veio 500".
        Assert.True(
            resposta.StatusCode == HttpStatusCode.Created,
            await resposta.Content.ReadAsStringAsync());

        var detalhe = await _api.Client.ObterNotaAsync(nota.Id);

        var item = Assert.Single(detalhe.Itens);
        Assert.Equal(produtoId, item.ProdutoId);
        Assert.Equal("CAF-001", item.Codigo);
        Assert.Equal("Café em grãos 1kg", item.Descricao);
        Assert.Equal(3, item.Quantidade);
    }

    [Fact]
    public async Task Quantidade_do_item_pode_ser_alterada()
    {
        var produtoId = _api.Estoque.Cadastrar("CAF-001", "Café em grãos 1kg", 10);
        var nota = await _api.Client.CriarNotaAsync();
        var item = await AdicionarItemAsync(nota.Id, produtoId, 3);

        var resposta = await _api.Client.PutAsJsonAsync(
            $"/notas-fiscais/{nota.Id}/itens/{item.Id}",
            new { quantidade = 7 });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var detalhe = await _api.Client.ObterNotaAsync(nota.Id);
        Assert.Equal(7, Assert.Single(detalhe.Itens).Quantidade);
    }

    [Fact]
    public async Task Item_removido_some_da_nota()
    {
        var produtoId = _api.Estoque.Cadastrar("CAF-001", "Café em grãos 1kg", 10);
        var nota = await _api.Client.CriarNotaAsync();
        var item = await AdicionarItemAsync(nota.Id, produtoId, 3);

        var resposta = await _api.Client.DeleteAsync($"/notas-fiscais/{nota.Id}/itens/{item.Id}");

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);

        var detalhe = await _api.Client.ObterNotaAsync(nota.Id);
        Assert.Empty(detalhe.Itens);
    }

    [Fact]
    public async Task Nota_aceita_varios_produtos_com_quantidades_diferentes()
    {
        var cafe = _api.Estoque.Cadastrar("CAF-001", "Café em grãos 1kg", 10);
        var acucar = _api.Estoque.Cadastrar("ACU-001", "Açúcar refinado 1kg", 20);
        var nota = await _api.Client.CriarNotaAsync();

        await AdicionarItemAsync(nota.Id, cafe, 3);
        await AdicionarItemAsync(nota.Id, acucar, 5);

        var detalhe = await _api.Client.ObterNotaAsync(nota.Id);

        Assert.Equal(2, detalhe.Itens.Length);
        Assert.Equal(5, detalhe.Itens.Single(item => item.Codigo == "ACU-001").Quantidade);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public async Task Quantidade_nao_positiva_e_recusada(int quantidade)
    {
        var produtoId = _api.Estoque.Cadastrar("CAF-001", "Café em grãos 1kg", 10);
        var nota = await _api.Client.CriarNotaAsync();

        var resposta = await _api.Client.PostAsJsonAsync(
            $"/notas-fiscais/{nota.Id}/itens",
            new { produtoId, quantidade });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);

        var detalhe = await _api.Client.ObterNotaAsync(nota.Id);
        Assert.Empty(detalhe.Itens);
    }

    [Fact]
    public async Task Produto_que_nao_existe_no_estoque_e_recusado()
    {
        var nota = await _api.Client.CriarNotaAsync();

        var resposta = await _api.Client.PostAsJsonAsync(
            $"/notas-fiscais/{nota.Id}/itens",
            new { produtoId = Guid.CreateVersion7(), quantidade = 1 });

        // 422 e não 404: a Nota Fiscal existe; o que não se sustenta é o Produto.
        Assert.Equal(HttpStatusCode.UnprocessableContent, resposta.StatusCode);
    }

    [Fact]
    public async Task Nota_fiscal_inexistente_responde_404()
    {
        var produtoId = _api.Estoque.Cadastrar("CAF-001", "Café em grãos 1kg", 10);

        var resposta = await _api.Client.PostAsJsonAsync(
            $"/notas-fiscais/{Guid.CreateVersion7()}/itens",
            new { produtoId, quantidade = 1 });

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
    }

    [Fact]
    public async Task Estoque_fora_do_ar_responde_503_em_vez_de_erro_opaco()
    {
        var nota = await _api.Client.CriarNotaAsync();
        _api.Estoque.ForaDoAr = true;

        var resposta = await _api.Client.PostAsJsonAsync(
            $"/notas-fiscais/{nota.Id}/itens",
            new { produtoId = Guid.CreateVersion7(), quantidade = 1 });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, resposta.StatusCode);
        Assert.Contains("Estoque", await resposta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Produtos_disponiveis_vem_do_estoque_pelo_faturamento()
    {
        _api.Estoque.Cadastrar("CAF-001", "Café em grãos 1kg", 10);
        _api.Estoque.Cadastrar("ACU-001", "Açúcar refinado 1kg", 20);

        var produtos = await _api.Client
            .GetFromJsonAsync<ProdutoDisponivelResponse[]>("/produtos-disponiveis");

        Assert.Equal(2, produtos!.Length);
        Assert.Contains(produtos, produto => produto.Codigo == "CAF-001");
    }

    [Fact]
    public async Task Produtos_disponiveis_responde_503_quando_o_estoque_esta_fora()
    {
        _api.Estoque.ForaDoAr = true;

        var resposta = await _api.Client.GetAsync("/produtos-disponiveis");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, resposta.StatusCode);
    }

    [Fact]
    public async Task Mesmo_produto_duas_vezes_e_recusado_com_mensagem_que_cita_o_codigo()
    {
        var produtoId = _api.Estoque.Cadastrar("CAF-001", "Café em grãos 1kg", 10);
        var nota = await _api.Client.CriarNotaAsync();
        await AdicionarItemAsync(nota.Id, produtoId, 3);

        var resposta = await _api.Client.PostAsJsonAsync(
            $"/notas-fiscais/{nota.Id}/itens",
            new { produtoId, quantidade = 2 });

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
        Assert.Contains("CAF-001", await resposta.Content.ReadAsStringAsync());
    }

    private async Task<ItemDaNotaResponse> AdicionarItemAsync(
        Guid notaId,
        Guid produtoId,
        int quantidade)
    {
        var resposta = await _api.Client.PostAsJsonAsync(
            $"/notas-fiscais/{notaId}/itens",
            new { produtoId, quantidade });

        Assert.True(
            resposta.StatusCode == HttpStatusCode.Created,
            await resposta.Content.ReadAsStringAsync());

        return (await resposta.Content.ReadFromJsonAsync<ItemDaNotaResponse>())!;
    }

    private sealed record ProdutoDisponivelResponse(Guid Id, string Codigo, string Descricao);
}
