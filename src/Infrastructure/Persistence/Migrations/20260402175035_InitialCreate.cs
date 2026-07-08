// Layer: Infrastructure
// Responsibility: Preserves EF Core migration history for the existing database schema.
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Greenlab.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LabId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Labs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DeviceKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    LightThreshold = table.Column<int>(type: "int", nullable: false),
                    TempFanOnThresholdCelsius = table.Column<float>(type: "real", nullable: false),
                    CvLabCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CameraRtspUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastOccupancyCount = table.Column<int>(type: "int", nullable: false),
                    LastOccupancyAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Labs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LabControlOverrides",
                columns: table => new
                {
                    LabId = table.Column<int>(type: "int", nullable: false),
                    MotionSensorDisabled = table.Column<bool>(type: "bit", nullable: false),
                    ForceLightOn = table.Column<bool>(type: "bit", nullable: true),
                    ForceFanOn = table.Column<bool>(type: "bit", nullable: true),
                    ForceRelay3On = table.Column<bool>(type: "bit", nullable: true),
                    ForceRelay4On = table.Column<bool>(type: "bit", nullable: true),
                    SetByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabControlOverrides", x => x.LabId);
                    table.ForeignKey(
                        name: "FK_LabControlOverrides_Labs_LabId",
                        column: x => x.LabId,
                        principalTable: "Labs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LabSectionSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LabId = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    CourseName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabSectionSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabSectionSchedules_Labs_LabId",
                        column: x => x.LabId,
                        principalTable: "Labs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LabChangeRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: false),
                    FromLabId = table.Column<int>(type: "int", nullable: false),
                    ToLabId = table.Column<int>(type: "int", nullable: false),
                    SectionStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SectionEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewedByUserId = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabChangeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabChangeRequests_Labs_FromLabId",
                        column: x => x.FromLabId,
                        principalTable: "Labs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabChangeRequests_Labs_ToLabId",
                        column: x => x.ToLabId,
                        principalTable: "Labs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabChangeRequests_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabChangeRequests_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleImageImports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    StoredPath = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedByUserId = table.Column<int>(type: "int", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleImageImports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleImageImports_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaffLabAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LabId = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: true),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffLabAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffLabAssignments_Labs_LabId",
                        column: x => x.LabId,
                        principalTable: "Labs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StaffLabAssignments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LabChangeRequests_FromLabId",
                table: "LabChangeRequests",
                column: "FromLabId");

            migrationBuilder.CreateIndex(
                name: "IX_LabChangeRequests_RequestedByUserId",
                table: "LabChangeRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LabChangeRequests_ReviewedByUserId",
                table: "LabChangeRequests",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LabChangeRequests_ToLabId",
                table: "LabChangeRequests",
                column: "ToLabId");

            migrationBuilder.CreateIndex(
                name: "IX_Labs_DeviceKey",
                table: "Labs",
                column: "DeviceKey");

            migrationBuilder.CreateIndex(
                name: "IX_LabSectionSchedules_LabId_DayOfWeek_StartTime",
                table: "LabSectionSchedules",
                columns: new[] { "LabId", "DayOfWeek", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleImageImports_UploadedByUserId",
                table: "ScheduleImageImports",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffLabAssignments_LabId",
                table: "StaffLabAssignments",
                column: "LabId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffLabAssignments_UserId_DayOfWeek_StartTime",
                table: "StaffLabAssignments",
                columns: new[] { "UserId", "DayOfWeek", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_StaffLabAssignments_UserId_ValidFrom_ValidUntil",
                table: "StaffLabAssignments",
                columns: new[] { "UserId", "ValidFrom", "ValidUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "LabChangeRequests");

            migrationBuilder.DropTable(
                name: "LabControlOverrides");

            migrationBuilder.DropTable(
                name: "LabSectionSchedules");

            migrationBuilder.DropTable(
                name: "ScheduleImageImports");

            migrationBuilder.DropTable(
                name: "StaffLabAssignments");

            migrationBuilder.DropTable(
                name: "Labs");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
