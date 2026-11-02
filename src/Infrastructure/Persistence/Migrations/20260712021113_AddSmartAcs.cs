using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlab.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSmartAcs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SmartAcs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LabId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false),
                    PathPrefix = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastTargetTempC = table.Column<int>(type: "int", nullable: true),
                    LastPowerOn = table.Column<bool>(type: "bit", nullable: true),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmartAcs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SmartAcs_Labs_LabId",
                        column: x => x.LabId,
                        principalTable: "Labs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SmartAcs_LabId",
                table: "SmartAcs",
                column: "LabId");

            migrationBuilder.CreateIndex(
                name: "IX_SmartAcs_LabId_IpAddress_Port",
                table: "SmartAcs",
                columns: new[] { "LabId", "IpAddress", "Port" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SmartAcs");
        }
    }
}
