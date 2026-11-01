using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatitasAPI.Migrations
{
    /// <inheritdoc />
    public partial class ShelterLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Shelters");

            migrationBuilder.AddColumn<bool>(
                name: "ShowAddress",
                table: "Shelters",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowAddress",
                table: "Shelters");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Shelters",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
