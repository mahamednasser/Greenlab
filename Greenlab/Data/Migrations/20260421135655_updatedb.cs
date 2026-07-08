using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlab.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StaffLabAssignments_LabId",
                table: "StaffLabAssignments");

            migrationBuilder.DropIndex(
                name: "IX_Labs_DeviceKey",
                table: "Labs");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignmentSource",
                table: "StaffLabAssignments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "StaffLabAssignments",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "AcademicTerm",
                table: "LabSectionSchedules",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "LabSectionSchedules",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "SectionName",
                table: "LabSectionSchedules",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CvLabCode",
                table: "Labs",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Labs",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddColumn<int>(
                name: "ControlMode",
                table: "LabControlOverrides",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "LabDevices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LabId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DeviceKeyHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DeviceType = table.Column<int>(type: "int", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabDevices_Labs_LabId",
                        column: x => x.LabId,
                        principalTable: "Labs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ManualControlCommands",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LabId = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    Target = table.Column<int>(type: "int", nullable: false),
                    RequestedValue = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CommandStatus = table.Column<int>(type: "int", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManualControlCommands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManualControlCommands_Labs_LabId",
                        column: x => x.LabId,
                        principalTable: "Labs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ManualControlCommands_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LabSensors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LabDeviceId = table.Column<int>(type: "int", nullable: false),
                    LabId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SensorType = table.Column<int>(type: "int", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabSensors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabSensors_LabDevices_LabDeviceId",
                        column: x => x.LabDeviceId,
                        principalTable: "LabDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LabSensors_Labs_LabId",
                        column: x => x.LabId,
                        principalTable: "Labs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StaffLabAssignments_LabId_DayOfWeek_StartTime",
                table: "StaffLabAssignments",
                columns: new[] { "LabId", "DayOfWeek", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_LabSectionSchedules_DayOfWeek_StartTime_EndTime",
                table: "LabSectionSchedules",
                columns: new[] { "DayOfWeek", "StartTime", "EndTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Labs_CvLabCode",
                table: "Labs",
                column: "CvLabCode");

            migrationBuilder.CreateIndex(
                name: "IX_Labs_DeviceKey",
                table: "Labs",
                column: "DeviceKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Labs_Name",
                table: "Labs",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabControlOverrides_SetByUserId",
                table: "LabControlOverrides",
                column: "SetByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Action",
                table: "AuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_LabId_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "LabId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LabDevices_DeviceKeyHash",
                table: "LabDevices",
                column: "DeviceKeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabDevices_LabId",
                table: "LabDevices",
                column: "LabId");

            migrationBuilder.CreateIndex(
                name: "IX_LabSensors_LabDeviceId",
                table: "LabSensors",
                column: "LabDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_LabSensors_LabId_SensorType",
                table: "LabSensors",
                columns: new[] { "LabId", "SensorType" });

            migrationBuilder.CreateIndex(
                name: "IX_ManualControlCommands_LabId_RequestedAt",
                table: "ManualControlCommands",
                columns: new[] { "LabId", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ManualControlCommands_RequestedByUserId",
                table: "ManualControlCommands",
                column: "RequestedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Labs_LabId",
                table: "AuditLogs",
                column: "LabId",
                principalTable: "Labs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Users_UserId",
                table: "AuditLogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_LabControlOverrides_Users_SetByUserId",
                table: "LabControlOverrides",
                column: "SetByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Labs_LabId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Users_UserId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_LabControlOverrides_Users_SetByUserId",
                table: "LabControlOverrides");

            migrationBuilder.DropTable(
                name: "LabSensors");

            migrationBuilder.DropTable(
                name: "ManualControlCommands");

            migrationBuilder.DropTable(
                name: "LabDevices");

            migrationBuilder.DropIndex(
                name: "IX_StaffLabAssignments_LabId_DayOfWeek_StartTime",
                table: "StaffLabAssignments");

            migrationBuilder.DropIndex(
                name: "IX_LabSectionSchedules_DayOfWeek_StartTime_EndTime",
                table: "LabSectionSchedules");

            migrationBuilder.DropIndex(
                name: "IX_Labs_CvLabCode",
                table: "Labs");

            migrationBuilder.DropIndex(
                name: "IX_Labs_DeviceKey",
                table: "Labs");

            migrationBuilder.DropIndex(
                name: "IX_Labs_Name",
                table: "Labs");

            migrationBuilder.DropIndex(
                name: "IX_LabControlOverrides_SetByUserId",
                table: "LabControlOverrides");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Action",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_LabId_CreatedAt",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_UserId_CreatedAt",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AssignmentSource",
                table: "StaffLabAssignments");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "StaffLabAssignments");

            migrationBuilder.DropColumn(
                name: "AcademicTerm",
                table: "LabSectionSchedules");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "LabSectionSchedules");

            migrationBuilder.DropColumn(
                name: "SectionName",
                table: "LabSectionSchedules");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Labs");

            migrationBuilder.DropColumn(
                name: "ControlMode",
                table: "LabControlOverrides");

            migrationBuilder.AlterColumn<string>(
                name: "CvLabCode",
                table: "Labs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffLabAssignments_LabId",
                table: "StaffLabAssignments",
                column: "LabId");

            migrationBuilder.CreateIndex(
                name: "IX_Labs_DeviceKey",
                table: "Labs",
                column: "DeviceKey");
        }
    }
}
