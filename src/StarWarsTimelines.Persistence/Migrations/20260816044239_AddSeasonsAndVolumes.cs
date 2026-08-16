using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StarWarsTimelines.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSeasonsAndVolumes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SourceMaterialUnits_SourceMaterialId_Number",
                table: "SourceMaterialUnits");

            migrationBuilder.AddColumn<int>(
                name: "GroupNumber",
                table: "SourceMaterialUnits",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceMaterialUnitId",
                table: "SourceMaterialEvents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SourceMaterialUnits_SourceMaterialId_GroupNumber_Number",
                table: "SourceMaterialUnits",
                columns: new[] { "SourceMaterialId", "GroupNumber", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SourceMaterialEvents_SourceMaterialUnitId",
                table: "SourceMaterialEvents",
                column: "SourceMaterialUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_SourceMaterialEvents_SourceMaterialUnits_SourceMaterialUnitId",
                table: "SourceMaterialEvents",
                column: "SourceMaterialUnitId",
                principalTable: "SourceMaterialUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SourceMaterialEvents_SourceMaterialUnits_SourceMaterialUnitId",
                table: "SourceMaterialEvents");

            migrationBuilder.DropIndex(
                name: "IX_SourceMaterialUnits_SourceMaterialId_GroupNumber_Number",
                table: "SourceMaterialUnits");

            migrationBuilder.DropIndex(
                name: "IX_SourceMaterialEvents_SourceMaterialUnitId",
                table: "SourceMaterialEvents");

            migrationBuilder.DropColumn(
                name: "GroupNumber",
                table: "SourceMaterialUnits");

            migrationBuilder.DropColumn(
                name: "SourceMaterialUnitId",
                table: "SourceMaterialEvents");

            migrationBuilder.CreateIndex(
                name: "IX_SourceMaterialUnits_SourceMaterialId_Number",
                table: "SourceMaterialUnits",
                columns: new[] { "SourceMaterialId", "Number" },
                unique: true);
        }
    }
}
