using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReyplastCoreAPI.Api.Infrastructure.Persistence.Migrations
{
    public partial class AddChannelGroups : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ChannelGroupId",
                table: "Channels",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChannelGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelGroups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Channels_ChannelGroupId",
                table: "Channels",
                column: "ChannelGroupId");

            migrationBuilder.CreateIndex(
                name: "UX_ChannelGroups_Name",
                table: "ChannelGroups",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Channels_ChannelGroups_ChannelGroupId",
                table: "Channels",
                column: "ChannelGroupId",
                principalTable: "ChannelGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Channels_ChannelGroups_ChannelGroupId",
                table: "Channels");

            migrationBuilder.DropTable(
                name: "ChannelGroups");

            migrationBuilder.DropIndex(
                name: "IX_Channels_ChannelGroupId",
                table: "Channels");

            migrationBuilder.DropColumn(
                name: "ChannelGroupId",
                table: "Channels");
        }
    }
}
