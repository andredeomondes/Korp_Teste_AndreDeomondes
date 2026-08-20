namespace Estoque;

/// <summary>
/// Recusas do Estoque, cada uma com um código estável. O texto é para o
/// operador e pode mudar; o código é para quem programa contra a API — no caso,
/// o Faturamento — e não muda.
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

    public const string CodigoDuplicado = "CODIGO_DUPLICADO";
    public const string SaldoInsuficiente = "SALDO_INSUFICIENTE";
    public const string ProdutoInexistente = "PRODUTO_INEXISTENTE";
}
