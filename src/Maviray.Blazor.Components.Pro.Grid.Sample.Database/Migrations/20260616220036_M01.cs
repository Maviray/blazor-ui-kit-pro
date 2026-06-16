using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Database.Migrations
{
    /// <inheritdoc />
    public partial class M01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuthArea",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthArea", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuthScope",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthScope", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DbSampleItemOne",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Integer = table.Column<int>(type: "int", nullable: false),
                    Decimal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Long = table.Column<long>(type: "bigint", nullable: false),
                    SampleEnum = table.Column<int>(type: "int", nullable: false),
                    Boolean = table.Column<bool>(type: "bit", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbSampleItemOne", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DbSampleItemFour",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScopeId = table.Column<int>(type: "int", nullable: true),
                    AuthAreaId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbSampleItemFour", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DbSampleItemFour_AuthArea_AuthAreaId",
                        column: x => x.AuthAreaId,
                        principalTable: "AuthArea",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DbSampleItemFour_AuthScope_ScopeId",
                        column: x => x.ScopeId,
                        principalTable: "AuthScope",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DbSampleItemThree",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScopeId = table.Column<int>(type: "int", nullable: true),
                    AuthAreaId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbSampleItemThree", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DbSampleItemThree_AuthArea_AuthAreaId",
                        column: x => x.AuthAreaId,
                        principalTable: "AuthArea",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DbSampleItemThree_AuthScope_ScopeId",
                        column: x => x.ScopeId,
                        principalTable: "AuthScope",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DbSampleItemTwo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DbSampleItemOneId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DbSampleItemTwo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DbSampleItemTwo_DbSampleItemOne_DbSampleItemOneId",
                        column: x => x.DbSampleItemOneId,
                        principalTable: "DbSampleItemOne",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DbSampleItemFour_AuthAreaId",
                table: "DbSampleItemFour",
                column: "AuthAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_DbSampleItemFour_ScopeId",
                table: "DbSampleItemFour",
                column: "ScopeId");

            migrationBuilder.CreateIndex(
                name: "IX_DbSampleItemThree_AuthAreaId",
                table: "DbSampleItemThree",
                column: "AuthAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_DbSampleItemThree_ScopeId",
                table: "DbSampleItemThree",
                column: "ScopeId");

            migrationBuilder.CreateIndex(
                name: "IX_DbSampleItemTwo_DbSampleItemOneId",
                table: "DbSampleItemTwo",
                column: "DbSampleItemOneId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DbSampleItemFour");

            migrationBuilder.DropTable(
                name: "DbSampleItemThree");

            migrationBuilder.DropTable(
                name: "DbSampleItemTwo");

            migrationBuilder.DropTable(
                name: "AuthArea");

            migrationBuilder.DropTable(
                name: "AuthScope");

            migrationBuilder.DropTable(
                name: "DbSampleItemOne");
        }
    }
}
