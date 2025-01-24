using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreamSchedule.Migrations
{
    /// <inheritdoc />
    public partial class EmoteMonitors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "privileges",
                table: "Users",
                newName: "Privileges");

            migrationBuilder.CreateTable(
                name: "EmoteMonitorChannels",
                columns: table => new
                {
                    ID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChannelID = table.Column<int>(type: "INTEGER", nullable: false),
                    ChannelName = table.Column<string>(type: "TEXT", nullable: false),
                    OutputChannelName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmoteMonitorChannels", x => x.ID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmoteMonitorChannels");

            migrationBuilder.RenameColumn(
                name: "Privileges",
                table: "Users",
                newName: "privileges");
        }
    }
}
