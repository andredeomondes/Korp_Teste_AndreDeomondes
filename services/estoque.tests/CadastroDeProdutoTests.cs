using System.Net;
using System.Net.Http.Json;

namespace Estoque.Tests;

public sealed class CadastroDeProdutoTests : IClassFixture<EstoqueApiFixture>, IAsyncLifetime
{
    private readonly EstoqueApiFixture _api;

    public CadastroDeProdutoTests(EstoqueApiFixture api) => _api = api;

    public Task InitializeAsync() => _api.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Produto_cadastrado_aparece_na_listagem_com_seu_saldo()
    {
        var resposta = await _api.Client.PostAsJsonAsync(
            "/produtos",
            new { codigo = "CAF-001", descricao = "Café em grãos 1kg", saldo = 10 });

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);

        var produtos = await _api.Client.GetFromJsonAsync<ProdutoResponse[]>("/produtos");

        var produto = Assert.Single(produtos!);
        Assert.Equal("CAF-001", produto.Codigo);
        Assert.Equal("Café em grãos 1kg", produto.Descricao);
        Assert.Equal(10, produto.Saldo);
    }

    [Fact]
    public async Task Location_do_produto_cadastrado_leva_ao_proprio_produto()
    {
        var resposta = await _api.Client.PostAsJsonAsync(
            "/produtos",
            new { codigo = "CAF-003", descricao = "Café coado", saldo = 4 });

        var location = resposta.Headers.Location;
        Assert.NotNull(location);

        var produto = await _api.Client.GetFromJsonAsync<ProdutoResponse>(location);

        Assert.Equal("CAF-003", produto!.Codigo);
    }

    [Fact]
    public async Task Codigo_duplicado_e_recusado_com_mensagem_que_cita_o_codigo()
    {
        await _api.Client.PostAsJsonAsync(
            "/produtos",
            new { codigo = "CAF-001", descricao = "Café em grãos 1kg", saldo = 10 });

        var resposta = await _api.Client.PostAsJsonAsync(
            "/produtos",
            new { codigo = "CAF-001", descricao = "Café moído 500g", saldo = 3 });

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);

        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Contains("CAF-001", problema!.Detail);

        // O primeiro Produto continua sendo o único cadastrado.
        var produtos = await _api.Client.GetFromJsonAsync<ProdutoResponse[]>("/produtos");
        Assert.Equal("Café em grãos 1kg", Assert.Single(produtos!).Descricao);
    }

    [Fact]
    public async Task Codigo_duplicado_ignora_diferenca_de_caixa()
    {
        await _api.Client.PostAsJsonAsync(
            "/produtos",
            new { codigo = "CAF-001", descricao = "Café em grãos 1kg", saldo = 10 });

        var resposta = await _api.Client.PostAsJsonAsync(
            "/produtos",
            new { codigo = "caf-001", descricao = "Café moído 500g", saldo = 3 });

        Assert.Equal(HttpStatusCode.Conflict, resposta.StatusCode);
    }

    [Fact]
    public async Task Codigo_em_branco_e_recusado()
    {
        var resposta = await _api.Client.PostAsJsonAsync(
            "/produtos",
            new { codigo = "   ", descricao = "Café sem código", saldo = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);

        var produtos = await _api.Client.GetFromJsonAsync<ProdutoResponse[]>("/produtos");
        Assert.Empty(produtos!);
    }

    [Fact]
    public async Task Saldo_negativo_e_recusado_e_nada_e_cadastrado()
    {
        var resposta = await _api.Client.PostAsJsonAsync(
            "/produtos",
            new { codigo = "CAF-002", descricao = "Café descafeinado", saldo = -1 });

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);

        var produtos = await _api.Client.GetFromJsonAsync<ProdutoResponse[]>("/produtos");
        Assert.Empty(produtos!);
    }

    private sealed record ProdutoResponse(Guid Id, string Codigo, string Descricao, int Saldo);

    private sealed record ProblemDetails(string Title, string Detail);
}
