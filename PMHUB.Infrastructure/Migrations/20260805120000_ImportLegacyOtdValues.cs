using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMHUB.Infrastructure.Migrations
{
    public partial class ImportLegacyOtdValues : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsManualValue",
                table: "KPIs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE [KPIs] SET [IsManualValue] = 1 WHERE [CurrentValue] > 0;");

            migrationBuilder.Sql("""
                DECLARE @legacyOtd TABLE ([ProjectName] nvarchar(150) NOT NULL, [Value] decimal(18,2) NOT NULL);
                INSERT INTO @legacyOtd ([ProjectName], [Value]) VALUES
                    (N'EHS Training Tracking APP', 84),
                    (N'EHS GLobal Platform', 71),
                    (N'Supply chain Dashboard ICT', 70),
                    (N'Visa Management', 83),
                    (N'Measure Diciplinary Enhancements', 100),
                    (N'COO-Country of origin PH1', 100),
                    (N'Digital Audit System( LPA, Saftey inspection, Quality…)', 100),
                    (N'MCA Digital Academy & Skill Matrix', 100),
                    (N'ABM Phase 1', 99),
                    (N'DTO For ICT wave1', 100),
                    (N'CSR-REFTRACK', 100),
                    (N'CS Ordering Optimization Phase 2', 97),
                    (N'Manufacturing Dashboard Toubkal', 100),
                    (N'Revenue Dashboard for CS', 99),
                    (N'CARPOOL', 100),
                    (N'Strategic Souring Phase 2', 0),
                    (N'PE- E-Drawing Phase 2', 97),
                    (N'EHS E- Dojo PH2', 100),
                    (N'Good Idea V3', 71),
                    (N'MTS Enhancements', 100),
                    (N'Manufacturing Dashboard TAC1', 100),
                    (N'Connectivity Technologies Profiling', 82),
                    (N'Sourcing Phase 3 AI', 100),
                    (N'Lab Genius PH2', 100),
                    (N'EHS E-Dojo PH3', 96);

                INSERT INTO [KPIs] ([Id], [ProjectId], [Name], [TargetValue], [CurrentValue], [IsManualValue], [EstimatedHours], [ActualHours], [CreatedAt])
                SELECT NEWID(), p.[Id], N'OTD', 85, o.[Value], 1, 0, 0, SYSUTCDATETIME()
                FROM [Projects] p
                INNER JOIN @legacyOtd o ON o.[ProjectName] = p.[Name]
                WHERE NOT EXISTS (
                    SELECT 1 FROM [KPIs] k
                    WHERE k.[ProjectId] = p.[Id] AND UPPER(k.[Name]) = N'OTD'
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE k
                FROM [KPIs] k
                INNER JOIN [Projects] p ON p.[Id] = k.[ProjectId]
                WHERE UPPER(k.[Name]) = N'OTD'
                  AND p.[Name] IN (
                    N'EHS Training Tracking APP', N'EHS GLobal Platform', N'Supply chain Dashboard ICT', N'Visa Management',
                    N'Measure Diciplinary Enhancements', N'COO-Country of origin PH1', N'Digital Audit System( LPA, Saftey inspection, Quality…)',
                    N'MCA Digital Academy & Skill Matrix', N'ABM Phase 1', N'DTO For ICT wave1', N'CSR-REFTRACK',
                    N'CS Ordering Optimization Phase 2', N'Manufacturing Dashboard Toubkal', N'Revenue Dashboard for CS',
                    N'CARPOOL', N'Strategic Souring Phase 2', N'PE- E-Drawing Phase 2', N'EHS E- Dojo PH2', N'Good Idea V3',
                    N'MTS Enhancements', N'Manufacturing Dashboard TAC1', N'Connectivity Technologies Profiling', N'Sourcing Phase 3 AI',
                    N'Lab Genius PH2', N'EHS E-Dojo PH3');
                """);

            migrationBuilder.DropColumn(name: "IsManualValue", table: "KPIs");
        }
    }
}
