using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BasketWorld.Migrations
{
    /// <inheritdoc />
    public partial class AddNbaSyncFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AwayScore",
                table: "Games",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExternalId",
                table: "Games",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomeScore",
                table: "Games",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Season",
                table: "Games",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Games",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Games",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AwayScore",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "HomeScore",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "Season",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Games");
        }
    }
}
