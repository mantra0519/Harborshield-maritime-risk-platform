using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HarborShield.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRiskCaseCachedExplanation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CachedExplanation",
                table: "risk_cases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExplanationCachedAt",
                table: "risk_cases",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CachedExplanation",
                table: "risk_cases");

            migrationBuilder.DropColumn(
                name: "ExplanationCachedAt",
                table: "risk_cases");
        }
    }
}
