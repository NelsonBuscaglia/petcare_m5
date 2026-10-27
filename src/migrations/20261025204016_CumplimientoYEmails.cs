using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetCare.migrations
{
    /// <inheritdoc />
    public partial class CumplimientoYEmails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReactivadaEnUtc",
                table: "Instancias",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "VencidaAlGenerarse",
                table: "Instancias",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "EmailsVencimiento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InstanciaId = table.Column<int>(type: "INTEGER", nullable: false),
                    AceptadoEnUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailsVencimiento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailsVencimiento_Instancias_InstanciaId",
                        column: x => x.InstanciaId,
                        principalTable: "Instancias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailsVencimiento_InstanciaId",
                table: "EmailsVencimiento",
                column: "InstanciaId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailsVencimiento");

            migrationBuilder.DropColumn(
                name: "ReactivadaEnUtc",
                table: "Instancias");

            migrationBuilder.DropColumn(
                name: "VencidaAlGenerarse",
                table: "Instancias");
        }
    }
}
