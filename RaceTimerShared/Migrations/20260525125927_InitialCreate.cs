using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceTimer.Shared.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Participants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    LastModifiedUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Participants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Races",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    StartTimeUTC = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true),
                    FinishDateTimeUTC = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastModifiedUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Races", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RaceParticipants",
                columns: table => new
                {
                    ParticipantID = table.Column<Guid>(type: "TEXT", nullable: false),
                    RaceID = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FinishDateTimeUTC = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastModifiedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ParticipantNr = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaceParticipants", x => new { x.ParticipantID, x.RaceID });
                    table.ForeignKey(
                        name: "FK_RaceParticipants_Participants_ParticipantID",
                        column: x => x.ParticipantID,
                        principalTable: "Participants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RaceParticipants_Races_RaceID",
                        column: x => x.RaceID,
                        principalTable: "Races",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RaceTimePoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    Index = table.Column<uint>(type: "INTEGER", nullable: false),
                    RaceID = table.Column<Guid>(type: "TEXT", nullable: false),
                    LastModifiedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    HasPenaltyTime = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaceTimePoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RaceTimePoints_Races_RaceID",
                        column: x => x.RaceID,
                        principalTable: "Races",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RaceParticipantTimePoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TimePointUTC = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PenaltyTime = table.Column<TimeSpan>(type: "TEXT", nullable: true),
                    RaceID = table.Column<Guid>(type: "TEXT", nullable: true),
                    ParticipantID = table.Column<Guid>(type: "TEXT", nullable: true),
                    RTPIndex = table.Column<uint>(type: "INTEGER", nullable: true),
                    RaceTimePointId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LastModifiedUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaceParticipantTimePoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RaceParticipantTimePoints_Participants_ParticipantID",
                        column: x => x.ParticipantID,
                        principalTable: "Participants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RaceParticipantTimePoints_RaceParticipants_ParticipantID_RaceID",
                        columns: x => new { x.ParticipantID, x.RaceID },
                        principalTable: "RaceParticipants",
                        principalColumns: new[] { "ParticipantID", "RaceID" });
                    table.ForeignKey(
                        name: "FK_RaceParticipantTimePoints_RaceTimePoints_RaceTimePointId",
                        column: x => x.RaceTimePointId,
                        principalTable: "RaceTimePoints",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RaceParticipantTimePoints_Races_RaceID",
                        column: x => x.RaceID,
                        principalTable: "Races",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RaceParticipants_RaceID",
                table: "RaceParticipants",
                column: "RaceID");

            migrationBuilder.CreateIndex(
                name: "IX_RaceParticipantTimePoints_ParticipantID_RaceID",
                table: "RaceParticipantTimePoints",
                columns: new[] { "ParticipantID", "RaceID" });

            migrationBuilder.CreateIndex(
                name: "IX_RaceParticipantTimePoints_RaceID",
                table: "RaceParticipantTimePoints",
                column: "RaceID");

            migrationBuilder.CreateIndex(
                name: "IX_RaceParticipantTimePoints_RaceTimePointId",
                table: "RaceParticipantTimePoints",
                column: "RaceTimePointId");

            migrationBuilder.CreateIndex(
                name: "IX_RaceTimePoints_RaceID",
                table: "RaceTimePoints",
                column: "RaceID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RaceParticipantTimePoints");

            migrationBuilder.DropTable(
                name: "RaceParticipants");

            migrationBuilder.DropTable(
                name: "RaceTimePoints");

            migrationBuilder.DropTable(
                name: "Participants");

            migrationBuilder.DropTable(
                name: "Races");
        }
    }
}
