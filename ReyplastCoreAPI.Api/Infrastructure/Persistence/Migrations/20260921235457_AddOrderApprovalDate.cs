using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReyplastCoreAPI.Api.Infrastructure.Persistence.Migrations
{
    public partial class AddOrderApprovalDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovalDate",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ApprovalDate_SituationCode",
                table: "Orders",
                columns: new[] { "ApprovalDate", "SituationCode" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_ApprovalDate_SituationCode",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ApprovalDate",
                table: "Orders");
        }
    }
}
