namespace Faturamento;

/// <summary>
/// Recusas do Faturamento, cada uma com um código estável. O texto é para o
/// operador e pode mudar; o código é para quem programa contra a API e não muda
/// — sem ele, distinguir "nota já impressa" de "nota sem itens" exigiria
/// comparar frases em português.
/// </summary>
public static class Erros
{
    public static IResult Recusa(
        string codigo,
        string titulo,
        string detalhe,
        int statusCode = StatusCodes.Status409Conflict) =>
        Results.Problem(
            title: titulo,
            detail: detalhe,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?> { ["codigo"] = codigo });

    public const string NotaJaImpressa = "NOTA_JA_IMPRESSA";
    public const string NotaSemItens = "NOTA_SEM_ITENS";
    public const string NotaFechada = "NOTA_FECHADA";
    public const string ImpressaoRecusada = "IMPRESSAO_RECUSADA";
    public const string ProdutoRepetido = "PRODUTO_REPETIDO";
    public const string ProdutoInexistente = "PRODUTO_INEXISTENTE";
    public const string EstoqueIndisponivel = "ESTOQUE_INDISPONIVEL";
}
