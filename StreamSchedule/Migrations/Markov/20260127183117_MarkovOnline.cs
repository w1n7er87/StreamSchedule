using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreamSchedule.Migrations.Markov
{
    /// <inheritdoc />
    public partial class MarkovOnline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            
            migrationBuilder.CreateTable(
                name: "TokenPairsOnline",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenPairsOnline", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TokenPairsOnline_TokenPairs_ID",
                        column: x => x.ID,
                        principalTable: "TokenPairs",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TokensOnline",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokensOnline", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TokensOnline_Tokens_ID",
                        column: x => x.ID,
                        principalTable: "Tokens",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TokenPairsOnline");

            migrationBuilder.DropTable(
                name: "TokensOnline");

            migrationBuilder.DropTable(
                name: "TokenPairs");

            migrationBuilder.DropTable(
                name: "Tokens");
        }
    }
}
