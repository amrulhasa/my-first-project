using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BDTechMarket.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrderDetailCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Count",
                table: "OrderDetails",
                newName: "Quantity");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "OrderDetails",
                newName: "Count");
        }
    }
}
