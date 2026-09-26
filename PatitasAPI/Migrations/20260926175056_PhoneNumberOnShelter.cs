using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PatitasAPI.Migrations
{
    /// <inheritdoc />
    public partial class PhoneNumberOnShelter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Shelters",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Shelters");
        }
    }
}
