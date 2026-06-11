using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolledgerSystem.Migrations
{
    /// <inheritdoc />
    public partial class feestructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FeeStructures",
                columns: table => new
                {
                    FeeStructureID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassTypeID = table.Column<int>(type: "int", nullable: false),
                    FeeTypeID = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AcademicYear = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeeStructures", x => x.FeeStructureID);
                    table.ForeignKey(
                        name: "FK_FeeStructures_ClassTypes_ClassTypeID",
                        column: x => x.ClassTypeID,
                        principalTable: "ClassTypes",
                        principalColumn: "ClassTypeID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeeStructures_FeeTypes_FeeTypeID",
                        column: x => x.FeeTypeID,
                        principalTable: "FeeTypes",
                        principalColumn: "FeeTypeID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeeStructures_ClassTypeID",
                table: "FeeStructures",
                column: "ClassTypeID");

            migrationBuilder.CreateIndex(
                name: "IX_FeeStructures_FeeTypeID",
                table: "FeeStructures",
                column: "FeeTypeID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeeStructures");
        }
    }
}
