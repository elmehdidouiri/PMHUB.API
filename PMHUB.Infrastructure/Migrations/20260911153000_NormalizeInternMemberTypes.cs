using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PMHUB.Infrastructure.Persistence;

#nullable disable

namespace PMHUB.Infrastructure.Migrations
{
    /// <summary>
    /// Existing intern records were also created as NormalUser accounts with the
    /// default Employee member type. Align those accounts with their Intern rows.
    /// </summary>
    [DbContext(typeof(PMHubDbContext))]
    [Migration("20260911153000_NormalizeInternMemberTypes")]
    public partial class NormalizeInternMemberTypes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE u
                SET u.MemberType = 3
                FROM dbo.Users AS u
                INNER JOIN dbo.Interns AS i
                    ON LOWER(LTRIM(RTRIM(i.Name))) = LOWER(LTRIM(RTRIM(CONCAT(u.FirstName, ' ', u.LastName))))
                WHERE u.UserType = 'NormalUser'
                  AND u.MemberType = 1;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE u
                SET u.MemberType = 1
                FROM dbo.Users AS u
                INNER JOIN dbo.Interns AS i
                    ON LOWER(LTRIM(RTRIM(i.Name))) = LOWER(LTRIM(RTRIM(CONCAT(u.FirstName, ' ', u.LastName))))
                WHERE u.UserType = 'NormalUser'
                  AND u.MemberType = 3;");
        }
    }
}
