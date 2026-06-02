using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RaceTimer.Shared.Migrations
{
    /// <inheritdoc />
    public partial class Corrections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CorrectedByUser",
                table: "RaceParticipantTimePoints",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CorrectedTimePointUTC",
                table: "RaceParticipantTimePoints",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrectionReason",
                table: "RaceParticipantTimePoints",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CorrectionTimestamp",
                table: "RaceParticipantTimePoints",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCorrected",
                table: "RaceParticipantTimePoints",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "OriginalTimePointUTC",
                table: "RaceParticipantTimePoints",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CorrectedByUser",
                table: "RaceParticipantTimePoints");

            migrationBuilder.DropColumn(
                name: "CorrectedTimePointUTC",
                table: "RaceParticipantTimePoints");

            migrationBuilder.DropColumn(
                name: "CorrectionReason",
                table: "RaceParticipantTimePoints");

            migrationBuilder.DropColumn(
                name: "CorrectionTimestamp",
                table: "RaceParticipantTimePoints");

            migrationBuilder.DropColumn(
                name: "IsCorrected",
                table: "RaceParticipantTimePoints");

            migrationBuilder.DropColumn(
                name: "OriginalTimePointUTC",
                table: "RaceParticipantTimePoints");
        }
    }
}
