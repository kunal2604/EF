using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EF.Migrations
{
    /// <inheritdoc />
    public partial class AddJoiningDateToEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "JoiningDate",
                table: "Employees",
                type: "date",
                nullable: false,
                defaultValueSql: "CURRENT_DATE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JoiningDate",
                table: "Employees");
        }
    }
}
