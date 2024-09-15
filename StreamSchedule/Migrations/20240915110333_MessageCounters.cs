using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreamSchedule.Migrations
{
    /// <inheritdoc />
    public partial class MessageCounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MessagesOffline",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MessagesOnline",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MessagesOffline",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MessagesOnline",
                table: "Users");
        }
    }
}
