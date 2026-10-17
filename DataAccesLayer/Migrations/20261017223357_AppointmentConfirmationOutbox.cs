using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccesLayer.Migrations
{
    /// <inheritdoc />
    public partial class AppointmentConfirmationOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppointmentEmailDeliveries_AppointmentId",
                table: "AppointmentEmailDeliveries");

            migrationBuilder.AddColumn<bool>(
                name: "IsConfirmation",
                table: "AppointmentEmailDeliveries",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentEmailDeliveries_AppointmentId_IsConfirmation",
                table: "AppointmentEmailDeliveries",
                columns: new[] { "AppointmentId", "IsConfirmation" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppointmentEmailDeliveries_AppointmentId_IsConfirmation",
                table: "AppointmentEmailDeliveries");

            migrationBuilder.DropColumn(
                name: "IsConfirmation",
                table: "AppointmentEmailDeliveries");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentEmailDeliveries_AppointmentId",
                table: "AppointmentEmailDeliveries",
                column: "AppointmentId",
                unique: true);
        }
    }
}
