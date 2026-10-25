using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PetCare.migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Hogares",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hogares", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cuidadores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HogarId = table.Column<int>(type: "INTEGER", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false, collation: "NOCASE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cuidadores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cuidadores_Hogares_HogarId",
                        column: x => x.HogarId,
                        principalTable: "Hogares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Mascotas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HogarId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, collation: "NOCASE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mascotas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mascotas_Hogares_HogarId",
                        column: x => x.HogarId,
                        principalTable: "Hogares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Deberes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MascotaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Hora = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    Repeticion = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaAlta = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deberes", x => x.Id);
                    table.CheckConstraint("CK_Deberes_Repeticion", "\"Repeticion\" IN (1, 2)");
                    table.CheckConstraint("CK_Deberes_Tipo", "\"Tipo\" IN (1, 2)");
                    table.ForeignKey(
                        name: "FK_Deberes_Mascotas_MascotaId",
                        column: x => x.MascotaId,
                        principalTable: "Mascotas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Instancias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeberId = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Hora = table.Column<TimeOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Instancias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Instancias_Deberes_DeberId",
                        column: x => x.DeberId,
                        principalTable: "Deberes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosCumplimiento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InstanciaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    HoraReal = table.Column<TimeOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosCumplimiento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosCumplimiento_Instancias_InstanciaId",
                        column: x => x.InstanciaId,
                        principalTable: "Instancias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Hogares",
                columns: new[] { "Id", "Nombre" },
                values: new object[] { 1, "Hogar" });

            migrationBuilder.InsertData(
                table: "Cuidadores",
                columns: new[] { "Id", "Email", "HogarId" },
                values: new object[,]
                {
                    { 1, "ana@ejemplo.com", 1 },
                    { 2, "beto@ejemplo.com", 1 }
                });

            migrationBuilder.InsertData(
                table: "Mascotas",
                columns: new[] { "Id", "HogarId", "Nombre" },
                values: new object[,]
                {
                    { 1, 1, "Firulais" },
                    { 2, 1, "Michi" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cuidadores_Email",
                table: "Cuidadores",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cuidadores_HogarId",
                table: "Cuidadores",
                column: "HogarId");

            migrationBuilder.CreateIndex(
                name: "IX_Deberes_MascotaId",
                table: "Deberes",
                column: "MascotaId");

            migrationBuilder.CreateIndex(
                name: "IX_Instancias_DeberId_Fecha",
                table: "Instancias",
                columns: new[] { "DeberId", "Fecha" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Instancias_Fecha",
                table: "Instancias",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_Mascotas_HogarId_Nombre",
                table: "Mascotas",
                columns: new[] { "HogarId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosCumplimiento_Fecha",
                table: "RegistrosCumplimiento",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosCumplimiento_InstanciaId",
                table: "RegistrosCumplimiento",
                column: "InstanciaId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cuidadores");

            migrationBuilder.DropTable(
                name: "RegistrosCumplimiento");

            migrationBuilder.DropTable(
                name: "Instancias");

            migrationBuilder.DropTable(
                name: "Deberes");

            migrationBuilder.DropTable(
                name: "Mascotas");

            migrationBuilder.DropTable(
                name: "Hogares");
        }
    }
}
