using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreamSchedule.Migrations
{
    /// <inheritdoc />
    public partial class CommandAliases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Aliases",
                table: "TextCommands",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CommandAliases",
                columns: table => new
                {
                    CommandName = table.Column<string>(type: "TEXT", nullable: false),
                    Aliases = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandAliases", x => x.CommandName);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommandAliases");

            migrationBuilder.DropColumn(
                name: "Aliases",
                table: "TextCommands");
        }
    }
}
