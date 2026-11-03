using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDS.Application.Migrations
{
    /// <inheritdoc />
    public partial class PlanningM10_RequirementUnifiedCodeRemoval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requirements_SupportCategories_SupportCategoryId",
                schema: "Planning",
                table: "Requirements");

            migrationBuilder.DropForeignKey(
                name: "FK_Requirements_UnifiedCodes_UnifiedCodeId",
                schema: "Planning",
                table: "Requirements");

            migrationBuilder.DropIndex(
                name: "IX_Requirements_SupportCategoryId",
                schema: "Planning",
                table: "Requirements");

            migrationBuilder.DropIndex(
                name: "IX_Requirements_UnifiedCodeId",
                schema: "Planning",
                table: "Requirements");

            migrationBuilder.DropColumn(
                name: "SupportCategoryId",
                schema: "Planning",
                table: "Requirements");

            migrationBuilder.DropColumn(
                name: "UnifiedCodeId",
                schema: "Planning",
                table: "Requirements");

            migrationBuilder.DropColumn(
                name: "UnifiedCodeName",
                schema: "Planning",
                table: "Requirements");

            migrationBuilder.AlterColumn<string>(
                name: "MasterProductName",
                schema: "Planning",
                table: "Requirements",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "MasterProductId",
                schema: "Planning",
                table: "Requirements",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Requirements_MasterProductStatus",
                schema: "Planning",
                table: "Requirements",
                columns: new[] { "MasterProductId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_Requirements_MasterProducts_MasterProductId",
                schema: "Planning",
                table: "Requirements",
                column: "MasterProductId",
                principalSchema: "Planning",
                principalTable: "MasterProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requirements_MasterProducts_MasterProductId",
                schema: "Planning",
                table: "Requirements");

            migrationBuilder.DropIndex(
                name: "IX_Requirements_MasterProductStatus",
                schema: "Planning",
                table: "Requirements");

            migrationBuilder.AlterColumn<string>(
                name: "MasterProductName",
                schema: "Planning",
                table: "Requirements",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(300)",
                oldMaxLength: 300);

            migrationBuilder.AlterColumn<Guid>(
                name: "MasterProductId",
                schema: "Planning",
                table: "Requirements",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "SupportCategoryId",
                schema: "Planning",
                table: "Requirements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UnifiedCodeId",
                schema: "Planning",
                table: "Requirements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnifiedCodeName",
                schema: "Planning",
                table: "Requirements",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Requirements_SupportCategoryId",
                schema: "Planning",
                table: "Requirements",
                column: "SupportCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Requirements_UnifiedCodeId",
                schema: "Planning",
                table: "Requirements",
                column: "UnifiedCodeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Requirements_SupportCategories_SupportCategoryId",
                schema: "Planning",
                table: "Requirements",
                column: "SupportCategoryId",
                principalSchema: "Planning",
                principalTable: "SupportCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Requirements_UnifiedCodes_UnifiedCodeId",
                schema: "Planning",
                table: "Requirements",
                column: "UnifiedCodeId",
                principalSchema: "Planning",
                principalTable: "UnifiedCodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
