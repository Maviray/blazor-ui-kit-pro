using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maviray.Blazor.Components.Pro.Grid.Sample.Database.Migrations
{
    /// <inheritdoc />
    public partial class M02 : Migration
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
                name: "Scope",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scope", x => x.Id);
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
                        name: "FK_DbSampleItemFour_Scope_ScopeId",
                        column: x => x.ScopeId,
                        principalTable: "Scope",
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
                        name: "FK_DbSampleItemThree_Scope_ScopeId",
                        column: x => x.ScopeId,
                        principalTable: "Scope",
                        principalColumn: "Id");
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DbSampleItemFour");

            migrationBuilder.DropTable(
                name: "DbSampleItemThree");

            migrationBuilder.DropTable(
                name: "AuthArea");

            migrationBuilder.DropTable(
                name: "Scope");
        }
    }
}
