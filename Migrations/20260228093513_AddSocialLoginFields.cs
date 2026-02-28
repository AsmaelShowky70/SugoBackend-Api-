using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SugoBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddSocialLoginFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SocialId",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SocialProvider",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SocialId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SocialProvider",
                table: "Users");
        }
    }
}
