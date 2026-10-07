using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce_1035.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class NewOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Shipping",
                table: "OrderHeaders",
                newName: "ShippingDate");

            migrationBuilder.AddColumn<string>(
                name: "OrderTotal",
                table: "OrderHeaders",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrderTotal",
                table: "OrderHeaders");

            migrationBuilder.RenameColumn(
                name: "ShippingDate",
                table: "OrderHeaders",
                newName: "Shipping");
        }
    }
}
