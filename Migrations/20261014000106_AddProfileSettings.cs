using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AncientServer.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                table: "Players",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Players",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "Players",
                type: "character varying(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "NotificationsEnabled",
                table: "Players",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "NotificationsEnabled",
                table: "Players");
        }
    }
}
