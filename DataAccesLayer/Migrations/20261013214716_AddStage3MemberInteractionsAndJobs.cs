using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccesLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddStage3MemberInteractionsAndJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlogFavorites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    BlogId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogFavorites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlogFavorites_AspNetUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BlogFavorites_Blogs_BlogId",
                        column: x => x.BlogId,
                        principalTable: "Blogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BlogReactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppUserId = table.Column<int>(type: "int", nullable: false),
                    BlogId = table.Column<int>(type: "int", nullable: false),
                    ReactionType = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogReactions", x => x.Id);
                    table.CheckConstraint("CK_BlogReactions_ReactionType", "[ReactionType] IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_BlogReactions_AspNetUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BlogReactions_Blogs_BlogId",
                        column: x => x.BlogId,
                        principalTable: "Blogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContactEmailDeliveries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContactFormId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MessageId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LeaseToken = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    LeaseExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HangfireJobId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastEnqueuedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactEmailDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactEmailDeliveries_ContactForms_ContactFormId",
                        column: x => x.ContactFormId,
                        principalTable: "ContactForms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrashArchiveEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrashArchiveEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlogFavorites_AppUserId_BlogId",
                table: "BlogFavorites",
                columns: new[] { "AppUserId", "BlogId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlogFavorites_BlogId",
                table: "BlogFavorites",
                column: "BlogId");

            migrationBuilder.CreateIndex(
                name: "IX_BlogReactions_AppUserId_BlogId",
                table: "BlogReactions",
                columns: new[] { "AppUserId", "BlogId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlogReactions_BlogId_ReactionType",
                table: "BlogReactions",
                columns: new[] { "BlogId", "ReactionType" });

            migrationBuilder.CreateIndex(
                name: "IX_ContactEmailDeliveries_ContactFormId",
                table: "ContactEmailDeliveries",
                column: "ContactFormId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContactEmailDeliveries_SentAtUtc_LastEnqueuedAtUtc",
                table: "ContactEmailDeliveries",
                columns: new[] { "SentAtUtc", "LastEnqueuedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TrashArchiveEntries_EntityType_EntityId",
                table: "TrashArchiveEntries",
                columns: new[] { "EntityType", "EntityId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlogFavorites");

            migrationBuilder.DropTable(
                name: "BlogReactions");

            migrationBuilder.DropTable(
                name: "ContactEmailDeliveries");

            migrationBuilder.DropTable(
                name: "TrashArchiveEntries");
        }
    }
}
