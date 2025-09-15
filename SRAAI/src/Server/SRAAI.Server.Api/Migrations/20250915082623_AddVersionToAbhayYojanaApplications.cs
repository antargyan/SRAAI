using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRAAI.Server.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVersionToAbhayYojanaApplications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Version",
                schema: "dbo",
                table: "AbhayYojanaApplications",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                schema: "dbo",
                table: "AbhayYojanaApplications");
        }
    }
}
