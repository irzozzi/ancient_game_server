using System;
using System.Collections.Generic;
using AncientServer.Models;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AncientServer.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Continents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Element = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CenterX = table.Column<float>(type: "real", nullable: false),
                    CenterZ = table.Column<float>(type: "real", nullable: false),
                    MaxPlayers = table.Column<int>(type: "integer", nullable: false),
                    CurrentPlayers = table.Column<int>(type: "integer", nullable: false),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    IsStarter = table.Column<bool>(type: "boolean", nullable: false),
                    IsCentral = table.Column<bool>(type: "boolean", nullable: false),
                    LevelRequirement = table.Column<int>(type: "integer", nullable: false),
                    BonusType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Continents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Username = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ContinentId = table.Column<int>(type: "integer", nullable: true),
                    SpawnX = table.Column<float>(type: "real", nullable: false),
                    SpawnZ = table.Column<float>(type: "real", nullable: false),
                    CurrentX = table.Column<float>(type: "real", nullable: false),
                    CurrentZ = table.Column<float>(type: "real", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLogin = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Experience = table.Column<int>(type: "integer", nullable: false),
                    GuildId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Platinum = table.Column<int>(type: "integer", nullable: false),
                    SpiritEnergy = table.Column<int>(type: "integer", nullable: false),
                    CityId = table.Column<int>(type: "integer", nullable: false),
                    SettlementLocalX = table.Column<float>(type: "real", nullable: false),
                    SettlementLocalZ = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContinentId = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    CenterX = table.Column<float>(type: "real", nullable: false),
                    CenterZ = table.Column<float>(type: "real", nullable: false),
                    CastleOwnerGuildId = table.Column<string>(type: "text", nullable: true),
                    Settlements = table.Column<List<PlayerSettlement>>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    Mines = table.Column<List<Mine>>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    MobCamps = table.Column<List<MobCamp>>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    Dungeons = table.Column<List<Dungeon>>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Buff = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cities_Continents_ContinentId",
                        column: x => x.ContinentId,
                        principalTable: "Continents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cities_ContinentId",
                table: "Cities",
                column: "ContinentId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_Username",
                table: "Players",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cities");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Continents");
        }
    }
}
