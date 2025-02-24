using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreamSchedule.Migrations
{
    /// <inheritdoc />
    public partial class PermittedTerms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PermittedTerms",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Term = table.Column<string>(type: "TEXT", nullable: false),
                    Noreplace = table.Column<bool>(type: "INTEGER", nullable: false),
                    Anycase = table.Column<bool>(type: "INTEGER", nullable: false),
                    Alternative = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermittedTerms", x => x.ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PermittedTerms");
        }
    }
}
