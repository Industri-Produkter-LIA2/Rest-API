using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPShop.Api.Migrations
{
    /// <inheritdoc />
    public partial class cleanCustomerModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Products_ProductId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "OrderNotifications");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Customers",
                newName: "OrgNumber");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "Customers",
                newName: "InvoiceAddress");

            migrationBuilder.RenameColumn(
                name: "Company",
                table: "Customers",
                newName: "CompanyName");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Products",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Carts_Customers_CustomerId",
                table: "Carts",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Products_ProductId",
                table: "OrderItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Carts_Customers_CustomerId",
                table: "Carts");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Products_ProductId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "OrgNumber",
                table: "Customers",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "InvoiceAddress",
                table: "Customers",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "CompanyName",
                table: "Customers",
                newName: "Company");

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "OrderNotifications",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Products_ProductId",
                table: "OrderItems",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
