using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StreamSchedule.Migrations
{
    /// <inheritdoc />
    public partial class StreamStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "Streams",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0)
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<int>(
                name: "StreamStatus",
                table: "Streams",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                WITH NumberedRows AS (
                    SELECT ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS RowNum, *
                    FROM Streams
                )
                UPDATE Streams
                SET Id = NumberedRows.RowNum
                FROM NumberedRows
                WHERE Streams.StreamDate = NumberedRows.StreamDate
            ");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Streams",
                table: "Streams");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Streams",
                table: "Streams",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Streams",
                table: "Streams");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Streams");

            migrationBuilder.DropColumn(
                name: "StreamStatus",
                table: "Streams");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Streams",
                table: "Streams",
                column: "StreamDate");
        }
    }
}
