using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoodSales.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestedDeliveryDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "RequestedDeliveryDate",
                table: "Orders",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequestedDeliveryDate",
                table: "Orders");
        }
    }
}
