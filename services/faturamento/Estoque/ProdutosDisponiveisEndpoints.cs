namespace Faturamento.Estoque;

public static class ProdutosDisponiveisEndpoints
{
    /// <summary>
    /// A tela de detalhe da Nota Fiscal precisa saber quais Produtos pode
    /// incluir. Quem responde é o Faturamento, mesmo sendo dado do Estoque: a
    /// dependência entre os serviços fica no servidor, e não no navegador, que
    /// assim conversa com um serviço só e vê uma indisponibilidade traduzida.
    /// </summary>
    public static IEndpointRouteBuilder MapProdutosDisponiveis(this IEndpointRouteBuilder app)
    {
        app.MapGet("/produtos-disponiveis", async (
            IEstoqueClient estoque,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await estoque.ListarProdutosAsync(cancellationToken));
            }
            catch (Exception erro) when (erro is HttpRequestException or TaskCanceledException)
            {
                return EstoqueIndisponivel();
            }
        })
            .WithName("ListarProdutosDisponiveis")
            .WithTags("Produtos disponíveis");

        return app;
    }

    /// <summary>
    /// O Estoque fora do ar não é falha desta aplicação: o operador precisa
    /// saber que pode tentar de novo, e não ver um 500 seco.
    /// </summary>
    public static IResult EstoqueIndisponivel() =>
        Results.Problem(
            title: "Estoque indisponível",
            detail: "Não foi possível falar com o Estoque. Tente novamente em instantes.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
}
