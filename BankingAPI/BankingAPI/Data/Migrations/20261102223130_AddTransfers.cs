using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CounterpartyAccountId",
                table: "LedgerEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TransferId",
                table: "LedgerEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_CounterpartyAccountId",
                table: "LedgerEntries",
                column: "CounterpartyAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_TransferId",
                table: "LedgerEntries",
                column: "TransferId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LedgerEntries_TransferShape",
                table: "LedgerEntries",
                sql: "([Type] IN ('TransferOut', 'TransferIn') AND [TransferId] IS NOT NULL AND [CounterpartyAccountId] IS NOT NULL\n    AND [CounterpartyAccountId] <> [AccountId])\nOR ([Type] IN ('Deposit', 'Withdrawal') AND [TransferId] IS NULL AND [CounterpartyAccountId] IS NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_LedgerEntries_Accounts_CounterpartyAccountId",
                table: "LedgerEntries",
                column: "CounterpartyAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LedgerEntries_Accounts_CounterpartyAccountId",
                table: "LedgerEntries");

            migrationBuilder.DropIndex(
                name: "IX_LedgerEntries_CounterpartyAccountId",
                table: "LedgerEntries");

            migrationBuilder.DropIndex(
                name: "IX_LedgerEntries_TransferId",
                table: "LedgerEntries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LedgerEntries_TransferShape",
                table: "LedgerEntries");

            migrationBuilder.DropColumn(
                name: "CounterpartyAccountId",
                table: "LedgerEntries");

            migrationBuilder.DropColumn(
                name: "TransferId",
                table: "LedgerEntries");
        }
    }
}
