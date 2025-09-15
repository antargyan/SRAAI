using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRAAI.Server.Api.Migrations
{
    /// <inheritdoc />
    public partial class MakeSerialNumberNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "SerialNumber",
                schema: "dbo",
                table: "AbhayYojanaApplications",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "SerialNumber",
                schema: "dbo",
                table: "AbhayYojanaApplications",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
