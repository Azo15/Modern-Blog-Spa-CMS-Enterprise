using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccesLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageReplyAndVisitorStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReplied",
                table: "ContactForms",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastRepliedAt",
                table: "ContactForms",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ContactReplies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContactFormId = table.Column<int>(type: "int", nullable: false),
                    ReplyMessage = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    RepliedByUserId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RepliedByUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RepliedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsMailSent = table.Column<bool>(type: "bit", nullable: false),
                    RecipientEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ClientRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MessageId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    LeaseToken = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    LeaseExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastEnqueuedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HangfireJobId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactReplies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactReplies_ContactForms_ContactFormId",
                        column: x => x.ContactFormId,
                        principalTable: "ContactForms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DailyVisitorStats",
                columns: table => new
                {
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalPageViews = table.Column<int>(type: "int", nullable: false),
                    UniqueVisitors = table.Column<int>(type: "int", nullable: false),
                    MobileVisits = table.Column<int>(type: "int", nullable: false),
                    DesktopVisits = table.Column<int>(type: "int", nullable: false),
                    TopVisitedPagesJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyVisitorStats", x => x.Date);
                });

            migrationBuilder.CreateTable(
                name: "VisitorLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    VisitorHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsMobile = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitorLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContactReplies_ClientRequestId",
                table: "ContactReplies",
                column: "ClientRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContactReplies_ContactFormId_RepliedAt",
                table: "ContactReplies",
                columns: new[] { "ContactFormId", "RepliedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ContactReplies_IsMailSent_LastEnqueuedAtUtc",
                table: "ContactReplies",
                columns: new[] { "IsMailSent", "LastEnqueuedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_VisitorLogs_CreatedDate",
                table: "VisitorLogs",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorLogs_Date_Path",
                table: "VisitorLogs",
                columns: new[] { "Date", "Path" });

            migrationBuilder.CreateIndex(
                name: "IX_VisitorLogs_Date_VisitorHash",
                table: "VisitorLogs",
                columns: new[] { "Date", "VisitorHash" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactReplies");

            migrationBuilder.DropTable(
                name: "DailyVisitorStats");

            migrationBuilder.DropTable(
                name: "VisitorLogs");

            migrationBuilder.DropColumn(
                name: "IsReplied",
                table: "ContactForms");

            migrationBuilder.DropColumn(
                name: "LastRepliedAt",
                table: "ContactForms");
        }
    }
}
