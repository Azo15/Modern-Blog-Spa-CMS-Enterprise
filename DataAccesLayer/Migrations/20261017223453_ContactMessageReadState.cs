using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccesLayer.Migrations
{
    /// <inheritdoc />
    public partial class ContactMessageReadState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRead",
                table: "ContactForms",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRead",
                table: "ContactForms");
        }
    }
}
