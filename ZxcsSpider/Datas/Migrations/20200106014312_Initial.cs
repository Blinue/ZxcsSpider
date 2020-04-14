using Microsoft.EntityFrameworkCore.Migrations;

namespace ZxcsSpider.Datas.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Books",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(nullable: true),
                    Author = table.Column<string>(nullable: true),
                    XianCao = table.Column<int>(nullable: false),
                    LiangCao = table.Column<int>(nullable: false),
                    GanCao = table.Column<int>(nullable: false),
                    KuCao = table.Column<int>(nullable: false),
                    DuCao = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Books", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Books");
        }
    }
}
