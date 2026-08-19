using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Faturamento.NotasFiscais.Migrations
{
    /// <inheritdoc />
    public partial class CriaNotasFiscais : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "numeracao_nota_fiscal");

            migrationBuilder.CreateTable(
                name: "notas_fiscais",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero = table.Column<long>(type: "bigint", nullable: false, defaultValueSql: "nextval('numeracao_nota_fiscal')"),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notas_fiscais", x => x.id);
                    table.CheckConstraint("ck_notas_fiscais_status", "status IN ('Aberta', 'Fechada')");
                });

            migrationBuilder.CreateIndex(
                name: "IX_notas_fiscais_numero",
                table: "notas_fiscais",
                column: "numero",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notas_fiscais");

            migrationBuilder.DropSequence(
                name: "numeracao_nota_fiscal");
        }
    }
}
