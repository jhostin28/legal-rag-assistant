using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LegalRag.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLawsAndRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "laws",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_laws", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_documents_law_id_effective_date",
                table: "documents",
                columns: new[] { "law_id", "effective_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_laws_name",
                table: "laws",
                column: "name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_documents_laws_law_id",
                table: "documents",
                column: "law_id",
                principalTable: "laws",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_documents_laws_law_id",
                table: "documents");

            migrationBuilder.DropTable(
                name: "laws");

            migrationBuilder.DropIndex(
                name: "ix_documents_law_id_effective_date",
                table: "documents");
        }
    }
}
