using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SRAAI.Server.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAbhayYojanaApplications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AbhayYojanaApplications",
                schema: "dbo",
                columns: table => new
                {
                    OriginalSlumNumber = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SerialNumber = table.Column<int>(type: "int", nullable: false),
                    OriginalSlumDwellerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ApplicantName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    VoterListYear = table.Column<int>(type: "int", nullable: true),
                    VoterListPartNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    VoterListSerialNumber = table.Column<int>(type: "int", nullable: true),
                    VoterListBound = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SlumUsage = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CarpetAreaSqFt = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    EvidenceDetails = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    EligibilityStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AbhayYojanaApplications", x => x.OriginalSlumNumber);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AbhayYojanaApplications",
                schema: "dbo");
        }
    }
}
