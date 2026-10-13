using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccesLayer.Migrations
{
    /// <inheritdoc />
    public partial class AddBlogPublishStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PublishStatus",
                table: "Blogs",
                type: "int",
                nullable: false,
                defaultValue: 0);
            // Preserve existing publication decisions instead of hiding all legacy posts.
            migrationBuilder.Sql("UPDATE [Blogs] SET [PublishStatus] = CASE WHEN [IsPublish] = 1 THEN 2 ELSE 0 END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublishStatus",
                table: "Blogs");
        }
    }
}
