using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Uala.Challenge.Infrastructure.DAL.Migrations
{
    public partial class SeedInitialUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
               schema: "Users", 
               table: "users", 
               columns: new[] { "username" },
               values: new object[,]
               {
                    { "Alice" },
                    { "Bob" },
                    { "Charlie" }
               });
        }
    }
}
