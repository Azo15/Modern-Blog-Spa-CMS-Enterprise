using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccesLayer.Migrations
{
    /// <inheritdoc />
    public partial class BackfillMissingBlogSlugs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fill only missing URLs; preserve all existing content, authors and valid slugs.
            migrationBuilder.Sql(@"UPDATE b SET [Slug] =
                CASE WHEN EXISTS (SELECT 1 FROM [Blogs] other WHERE other.[Id] <> b.[Id] AND other.[Slug] = CONCAT('blog-', b.[Id]))
                THEN CONCAT('blog-', b.[Id], '-', CONVERT(nvarchar(36), NEWID()))
                ELSE CONCAT('blog-', b.[Id]) END
                FROM [Blogs] b WHERE b.[Slug] IS NULL OR LTRIM(RTRIM(b.[Slug])) = '';");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Keep repaired URLs on rollback; removing them would break published links.

        }
    }
}
