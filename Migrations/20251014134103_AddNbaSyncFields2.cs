using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BasketWorld.Migrations
{
    /// <inheritdoc />
    public partial class AddNbaSyncFields2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Abbreviation",
                table: "Teams",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExternalId",
                table: "Teams",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Abbreviation",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Teams");
        }
    }
}
