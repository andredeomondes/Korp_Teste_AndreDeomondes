using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Estoque.Produtos;

public sealed record CadastrarProdutoRequest(string Codigo, string Descricao, int Saldo);

public sealed record ProdutoResponse(Guid Id, string Codigo, string Descricao, int Saldo);

public static class ProdutosEndpoints
{
    public static IEndpointRouteBuilder MapProdutos(this IEndpointRouteBuilder app)
    {
        var produtos = app.MapGroup("/produtos").WithTags("Produtos");

        produtos.MapGet("/", async (EstoqueDbContext db, CancellationToken cancellationToken) =>
            await db.Produtos
                .OrderBy(produto => produto.Codigo)
                .Select(produto => new ProdutoResponse(
                    produto.Id,
                    produto.Codigo,
                    produto.Descricao,
                    produto.Saldo))
                .ToListAsync(cancellationToken))
            .WithName("ListarProdutos");

        produtos.MapGet("/{id:guid}", async (
            Guid id,
            EstoqueDbContext db,
            CancellationToken cancellationToken) =>
        {
            var produto = await db.Produtos.FindAsync([id], cancellationToken);

            return produto is null
                ? Results.NotFound()
                : Results.Ok(new ProdutoResponse(
                    produto.Id,
                    produto.Codigo,
                    produto.Descricao,
                    produto.Saldo));
        })
            .WithName("ObterProduto");

        produtos.MapPost("/", async (
            CadastrarProdutoRequest request,
            EstoqueDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (Validar(request) is { Count: > 0 } erros)
            {
                return Results.ValidationProblem(erros);
            }

            var produto = new Produto
            {
                // Código é identificador, não texto livre: 'caf-001' e 'CAF-001'
                // designam o mesmo Produto, então normalizar antes de gravar faz
                // o índice único enxergar a duplicidade.
                Codigo = request.Codigo.Trim().ToUpperInvariant(),
                Descricao = request.Descricao.Trim(),
                Saldo = request.Saldo,
            };

            db.Produtos.Add(produto);

            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException erro) when (ViolouUnicidadeDoCodigo(erro))
            {
                // A unicidade é decidida pelo banco, não por uma consulta prévia:
                // entre o SELECT e o INSERT outra requisição poderia cadastrar o
                // mesmo Código, e o índice único é o único juiz confiável.
                return Erros.Recusa(
                    Erros.CodigoDuplicado,
                    "Código duplicado",
                    $"Já existe um Produto com o Código '{produto.Codigo}'.");
            }

            var response = new ProdutoResponse(
                produto.Id,
                produto.Codigo,
                produto.Descricao,
                produto.Saldo);

            return Results.Created($"/produtos/{produto.Id}", response);
        })
            .WithName("CadastrarProduto");

        return app;
    }

    /// <summary>
    /// Recusa o cadastro antes de tocar no banco. O Saldo negativo também é
    /// barrado por restrição no schema; aqui a checagem existe para devolver ao
    /// operador uma mensagem por campo, e não um erro genérico de banco.
    /// </summary>
    private static Dictionary<string, string[]> Validar(CadastrarProdutoRequest request)
    {
        var erros = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Codigo))
        {
            erros[nameof(request.Codigo)] = ["Código é obrigatório."];
        }

        if (string.IsNullOrWhiteSpace(request.Descricao))
        {
            erros[nameof(request.Descricao)] = ["Descrição é obrigatória."];
        }

        if (request.Saldo < 0)
        {
            erros[nameof(request.Saldo)] = ["Saldo não pode ser negativo."];
        }

        return erros;
    }

    private static bool ViolouUnicidadeDoCodigo(DbUpdateException erro) =>
        erro.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
