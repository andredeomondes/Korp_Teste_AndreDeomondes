using Microsoft.EntityFrameworkCore;

namespace Estoque.Produtos;

public sealed class EstoqueDbContext(DbContextOptions<EstoqueDbContext> options) : DbContext(options)
{
    public DbSet<Produto> Produtos => Set<Produto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Produto>(produto =>
        {
            produto.ToTable("produtos", table => table.HasCheckConstraint(
                "ck_produtos_saldo_nao_negativo",
                "saldo >= 0"));

            produto.HasKey(p => p.Id);

            produto.Property(p => p.Id).HasColumnName("id");
            produto.Property(p => p.Codigo).HasColumnName("codigo").HasMaxLength(50);
            produto.Property(p => p.Descricao).HasColumnName("descricao").HasMaxLength(200);
            produto.Property(p => p.Saldo).HasColumnName("saldo");

            // Código identifica o Produto para o operador: dois Produtos com o
            // mesmo Código tornariam a listagem ambígua, então o banco recusa.
            produto.HasIndex(p => p.Codigo).IsUnique();
        });
    }
}
