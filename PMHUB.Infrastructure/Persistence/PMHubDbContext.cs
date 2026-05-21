using Microsoft.EntityFrameworkCore;
using PMHUB.Domain.Entities;

namespace PMHUB.Infrastructure.Persistence
{
    public class PMHubDbContext : DbContext
    {
        public PMHubDbContext(DbContextOptions<PMHubDbContext> options)
            : base(options) { }

        // ── Users TPH  
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<NormalUser> NormalUsers { get; set; } = null!;
        public DbSet<Admin> AdminUsers { get; set; } = null!;

        // ── Roles  
        public DbSet<Role> Roles { get; set; } = null!;

        // ── Intern  
        public DbSet<Intern> Interns { get; set; } = null!;

        // ── Core  
        public DbSet<BusinessUnit> BusinessUnits { get; set; } = null!;
        public DbSet<Department> Departments { get; set; } = null!;
        public DbSet<Plant> Plants { get; set; } = null!;
        public DbSet<Project> Projects { get; set; } = null!;

        // ── Sprint & Tasks  
        public DbSet<ProjectTask> Tasks { get; set; } = null!;
        public DbSet<Sprint> Sprints { get; set; } = null!;

        // ── Deliverables Breakdown
        public DbSet<DeliverableBreakdown> DeliverableBreakdowns { get; set; } = null!;
        public DbSet<DeliverableTask> DeliverableTasks { get; set; } = null!;

        // ── Timeline & Roadblocks
        public DbSet<ProjectTimelineEntry> ProjectTimelineEntries { get; set; } = null!;
        public DbSet<ProjectRoadblock> ProjectRoadblocks { get; set; } = null!;

        // ── Allocations  
        public DbSet<HourEntry> HourEntries { get; set; } = null!;
         public DbSet<InternAllocation> InternAllocations { get; set; } = null!;
        public DbSet<InternHourEntry> InternHourEntries { get; set; } = null!;
        public DbSet<HourEntryInternSupervision> HourEntryInternSupervisions { get; set; } = null!;
 
        // ── Autres  
        public DbSet<KPI> KPIs { get; set; } = null!;
        public DbSet<Report> Reports { get; set; } = null!;
        public DbSet<ProjectFile> ProjectFiles { get; set; } = null!;
        public DbSet<ProjectResource> ProjectResources { get; set; } = null!;
        public DbSet<StrategicCriterion> StrategicCriteria { get; set; } = null!;
        public DbSet<ProjectFileVersion> ProjectFileVersions { get; set; } = null!;

        // ── Many-to-Many  
        public DbSet<ProjectBusinessUnit> ProjectBusinessUnits { get; set; } = null!;
        public DbSet<ProjectDepartment> ProjectDepartments { get; set; } = null!;
        public DbSet<ProjectTechnology> ProjectTechnologies { get; set; } = null!;
        public DbSet<ProjectSolutionDomain> ProjectSolutionDomains { get; set; } = null!;
        public DbSet<SolutionDomain> SolutionDomains { get; set; } = null!;
        public DbSet<Technology> Technologies { get; set; } = null!;
        public DbSet<ProjectMember> ProjectMembers { get; set; } = null!;
        public DbSet<UserHourlyRate> UserHourlyRates { get; set; }
        public DbSet<Holiday> Holidays { get; set; }
        // ── Audit  
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<PasswordResetCode> PasswordResetCodes { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ── TPH Configuration  
            builder.Entity<User>()
                .HasDiscriminator<string>("UserType")
                .HasValue<NormalUser>("NormalUser")
                .HasValue<Admin>("AdminUser");

            // ── Email unique  
            builder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            builder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<RefreshToken>()
                .HasIndex(rt => rt.TokenHash)
                .IsUnique();

            builder.Entity<PasswordResetCode>()
                .HasOne(prc => prc.User)
                .WithMany()
                .HasForeignKey(prc => prc.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PasswordResetCode>()
                .HasIndex(prc => prc.UserId);

            builder.Entity<PasswordResetCode>()
                .HasIndex(prc => prc.ResetTokenHash)
                .IsUnique()
                .HasFilter("[ResetTokenHash] IS NOT NULL");

            // ── Role  
            builder.Entity<Role>()
                .HasIndex(r => r.Name)
                .IsUnique();

            // ── NormalUser → Role  
            builder.Entity<NormalUser>()
             .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── NormalUser → ApprovedBy (AdminUser)  
            builder.Entity<NormalUser>()
            .HasOne(u => u.ApprovedBy)
                .WithMany()
            .HasForeignKey(u => u.ApprovedById)
                    .OnDelete(DeleteBehavior.Restrict);

            // ── Intern  
            builder.Entity<Intern>()
                .HasOne(i => i.Role)
                .WithMany()
                .HasForeignKey(i => i.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Intern>()
                .HasOne(i => i.Supervisor)
                .WithMany()
                .HasForeignKey(i => i.SupervisorId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── BusinessUnit → Departments  
            builder.Entity<BusinessUnit>()
                .HasMany(bu => bu.Departments)
                .WithOne(d => d.BusinessUnit)
                .HasForeignKey(d => d.BusinessUnitId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Plant → Departments  
            builder.Entity<Plant>()
                .HasMany(p => p.Departments)
                .WithOne(d => d.Plant)
                .HasForeignKey(d => d.PlantId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Department → Projects  
            builder.Entity<Department>()
                .HasMany(d => d.Projects)
                .WithOne(p => p.Department)
                .HasForeignKey(p => p.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
 
            // ── Project self-referencing  
            builder.Entity<Project>()
                .HasOne(p => p.ParentProject)
                .WithMany(p => p.SubProjects)
                .HasForeignKey(p => p.ParentProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Project → ProjectManager (NormalUser)  
            builder.Entity<Project>()
                .HasOne(p => p.ProjectManager)
                .WithMany()
                .HasForeignKey(p => p.ProjectManagerId)
                .OnDelete(DeleteBehavior.SetNull);

            // ── Project → Sprints  
            builder.Entity<Sprint>()
                .HasOne(s => s.Project)
                .WithMany()
                .HasForeignKey(s => s.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Project → Tasks  
            builder.Entity<Project>()
                .HasMany(p => p.Tasks)
                .WithOne(t => t.Project)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Project → Deliverables
            builder.Entity<Project>()
                .HasMany(p => p.Deliverables)
                .WithOne(d => d.Project)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Project → Timeline entries
            builder.Entity<Project>()
                .HasMany(p => p.TimelineEntries)
                .WithOne(t => t.Project)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Project → Roadblocks
            builder.Entity<Project>()
                .HasMany(p => p.RoadblockEntries)
                .WithOne(r => r.Project)
                .HasForeignKey(r => r.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Deliverables → Deliverable Tasks
            builder.Entity<DeliverableBreakdown>()
                .HasMany(d => d.Tasks)
                .WithOne(t => t.Deliverable)
                .HasForeignKey(t => t.DeliverableId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Sprint → Tasks  
            builder.Entity<ProjectTask>()
                .HasOne(t => t.Sprint)
                .WithMany(s => s.Tasks)
                .HasForeignKey(t => t.SprintId)
                .OnDelete(DeleteBehavior.SetNull);

            // ── Task → AssignedUser  
            builder.Entity<ProjectTask>()
                .HasOne(t => t.AssignedUser)
                .WithMany()
                .HasForeignKey(t => t.AssignedUserId)
                .OnDelete(DeleteBehavior.SetNull);

          

            // ── Project → Allocations  
       

            builder.Entity<Project>()
                .HasMany(p => p.InternAllocations)
                .WithOne(ia => ia.Project)
                .HasForeignKey(ia => ia.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<InternAllocation>()
                .HasOne(ia => ia.Intern)
                .WithMany()
                .HasForeignKey(ia => ia.InternId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<InternAllocation>()
                .HasMany(ia => ia.InternHourEntries)
                .WithOne(ihe => ihe.InternAllocation)
                .HasForeignKey(ihe => ihe.InternAllocationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<InternHourEntry>()
                .HasOne(ihe => ihe.BookedByUser)
                .WithMany()
                .HasForeignKey(ihe => ihe.BookedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Project → HourEntries  
            builder.Entity<Project>()
                .HasMany(p => p.HourEntries)
                .WithOne(h => h.Project)
                .HasForeignKey(h => h.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);



            // ── HourEntry → NormalUser  
            builder.Entity<HourEntry>()
                .HasOne(h => h.User)
                .WithMany(u => u.HourEntries)
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── HourEntry → InternSupervisions
            builder.Entity<HourEntryInternSupervision>()
                .HasOne(s => s.HourEntry)
                .WithMany(h => h.InternSupervisions)
                .HasForeignKey(s => s.HourEntryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<HourEntryInternSupervision>()
                .HasOne(s => s.InternAllocation)
                .WithMany()
                .HasForeignKey(s => s.InternAllocationId)
                .OnDelete(DeleteBehavior.Restrict);
 
            // ── Project → collections  
            builder.Entity<Project>()
                .HasMany(p => p.KPIs)
                .WithOne(k => k.Project)
                .HasForeignKey(k => k.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── DeliverableTask → Assigned resource (ProjectMember / InternAllocation)
            builder.Entity<DeliverableTask>()
                .HasOne(t => t.ProjectMember)
                .WithMany()
                .HasForeignKey(t => t.ProjectMemberId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<DeliverableTask>()
                .HasOne(t => t.InternAllocation)
                .WithMany()
                .HasForeignKey(t => t.InternAllocationId)
                .OnDelete(DeleteBehavior.SetNull);

            // ── Timeline sponsor (NormalUser)
            builder.Entity<ProjectTimelineEntry>()
                .HasOne(t => t.SponsorUser)
                .WithMany()
                .HasForeignKey(t => t.SponsorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Project>()
                .HasMany(p => p.ProjectFiles)
                .WithOne(f => f.Project)
                .HasForeignKey(f => f.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // ProjectFile → versions (historique)
            builder.Entity<ProjectFile>()
                .HasMany(f => f.Versions)
                .WithOne(v => v.ProjectFile)
                .HasForeignKey(v => v.ProjectFileId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Project>()
                .HasMany(p => p.ProjectResources)
                .WithOne(r => r.Project)
                .HasForeignKey(r => r.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Project>()
                .HasMany(p => p.StrategicCriteria)
                .WithOne(sc => sc.Project)
                .HasForeignKey(sc => sc.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Report → NormalUser  
            builder.Entity<Report>()
             .HasOne(r => r.CreatedBy)
                 .WithMany(u => u.Reports)       
                    .HasForeignKey(r => r.CreatedById)
                    .OnDelete(DeleteBehavior.Cascade);

            // ── ProjectMember  
            builder.Entity<ProjectMember>()
                .HasOne(pm => pm.Project)
                .WithMany(p => p.ProjectMembers)
                .HasForeignKey(pm => pm.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProjectMember>()
                .HasOne(pm => pm.User)
                .WithMany(u => u.ProjectMembers)  
                .HasForeignKey(pm => pm.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

            // ── ProjectMember → Role  
            builder.Entity<ProjectMember>()
                .HasOne(pm => pm.Role)
                .WithMany()
                .HasForeignKey(pm => pm.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProjectMember>()
                .HasIndex(pm => new { pm.ProjectId, pm.UserId })
                .IsUnique();

            // ── Many-to-Many : Project ↔ BusinessUnit  
            builder.Entity<ProjectBusinessUnit>()
                .HasKey(pbu => new { pbu.ProjectId, pbu.BusinessUnitId });
            builder.Entity<ProjectBusinessUnit>()
                .HasOne(pbu => pbu.Project)
                .WithMany(p => p.ProjectBusinessUnits)
                .HasForeignKey(pbu => pbu.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ProjectBusinessUnit>()
                .HasOne(pbu => pbu.BusinessUnit)
                .WithMany(bu => bu.ProjectBusinessUnits)
                .HasForeignKey(pbu => pbu.BusinessUnitId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Many-to-Many : Project ↔ Technology  
            builder.Entity<ProjectDepartment>()
                .HasKey(pd => new { pd.ProjectId, pd.DepartmentId });
            builder.Entity<ProjectDepartment>()
                .HasOne(pd => pd.Project)
                .WithMany(p => p.ProjectDepartments)
                .HasForeignKey(pd => pd.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ProjectDepartment>()
                .HasOne(pd => pd.Department)
                .WithMany(d => d.ProjectDepartments)
                .HasForeignKey(pd => pd.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProjectTechnology>()
                .HasKey(pt => new { pt.ProjectId, pt.TechnologyId });
            builder.Entity<ProjectTechnology>()
                .HasOne(pt => pt.Project)
                .WithMany(p => p.ProjectTechnologies)
                .HasForeignKey(pt => pt.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ProjectTechnology>()
                .HasOne(pt => pt.Technology)
                .WithMany(t => t.ProjectTechnologies)
                .HasForeignKey(pt => pt.TechnologyId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Many-to-Many : Project ↔ SolutionDomain  
            builder.Entity<ProjectSolutionDomain>()
                .HasKey(psd => new { psd.ProjectId, psd.SolutionDomainId });
            builder.Entity<ProjectSolutionDomain>()
                .HasOne(psd => psd.Project)
                .WithMany(p => p.ProjectSolutionDomains)
                .HasForeignKey(psd => psd.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Entity<ProjectSolutionDomain>()
                .HasOne(psd => psd.SolutionDomain)
                .WithMany(sd => sd.ProjectSolutionDomains)
                .HasForeignKey(psd => psd.SolutionDomainId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── UserHourlyRate → NormalUser
                builder.Entity<UserHourlyRate>()
                 .HasOne(r => r.User)
                    .WithMany()
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserHourlyRate>()
                .HasIndex(r => new { r.UserId, r.EffectiveFrom });

            builder.Entity<UserHourlyRate>()
                .Property(r => r.NormalRateAmount)
                .HasColumnType("decimal(10,2)");

            builder.Entity<UserHourlyRate>()
                .Property(r => r.PremiumRateAmount)
                .HasColumnType("decimal(10,2)");

            // ── Holiday
            builder.Entity<Holiday>()
                .HasIndex(h => h.Date);

            builder.Entity<Holiday>()
                .HasIndex(h => new { h.Date, h.Country });

            // ── Index performances  
            builder.Entity<Department>().HasIndex(d => d.BusinessUnitId);
            builder.Entity<Department>().HasIndex(d => d.PlantId);
            builder.Entity<Project>().HasIndex(p => p.DepartmentId);
            builder.Entity<Project>().HasIndex(p => p.Status);
            builder.Entity<Project>().HasIndex(p => p.Phase);
            builder.Entity<Sprint>().HasIndex(s => s.ProjectId);
            builder.Entity<ProjectTask>().HasIndex(t => t.ProjectId);
            builder.Entity<ProjectTask>().HasIndex(t => t.SprintId);
            builder.Entity<HourEntry>().HasIndex(h => h.ProjectId);
 
            // ── Decimal precision  
            builder.Entity<Project>()
                .Property(p => p.Budget)
                .HasColumnType("decimal(18,2)");

            // ── Default values  
            builder.Entity<Project>()
                .Property(p => p.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Entity<ProjectResource>()
                .Property(r => r.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
    

            builder.Entity<InternAllocation>()
                .Property(i => i.AllocatedHours)
                .HasColumnType("decimal(18,2)");

            builder.Entity<InternAllocation>()
                .Property(i => i.HoursWorked)
                .HasColumnType("decimal(18,2)");

            builder.Entity<InternAllocation>()
                .HasIndex(i => new { i.ProjectId, i.InternId })
                .IsUnique();

            builder.Entity<InternHourEntry>()
                .Property(i => i.Hours)
                .HasColumnType("decimal(5,2)");

            builder.Entity<InternHourEntry>()
                .HasIndex(i => i.InternAllocationId);

            builder.Entity<InternHourEntry>()
                .HasIndex(i => new { i.InternAllocationId, i.Date });

            builder.Entity<Project>()
                .Property(p => p.EstimatedHours)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Project>()
                .Property(p => p.ActualHours)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Project>()
                .Property(p => p.StrategicScore)
                .HasColumnType("decimal(18,2)");

            builder.Entity<DeliverableTask>()
                .Property(t => t.DevHours)
                .HasColumnType("decimal(18,2)");

            builder.Entity<DeliverableTask>()
                .Property(t => t.UxHours)
                .HasColumnType("decimal(18,2)");

            builder.Entity<DeliverableTask>()
                .Property(t => t.TestingHours)
                .HasColumnType("decimal(18,2)");

            builder.Entity<DeliverableTask>()
                .Property(t => t.Hours)
                .HasColumnType("decimal(18,2)");

            builder.Entity<DeliverableTask>()
                .Property(t => t.EstimatedHours)
                .HasColumnType("decimal(18,2)");
 
             builder.Entity<HourEntry>()
                .Property(h => h.ExecutionHours)
                .HasColumnType("decimal(5,2)");

            builder.Entity<HourEntry>()
                .Property(h => h.SupervisionHours)
                .HasColumnType("decimal(5,2)");

            builder.Entity<HourEntry>()
                .Property(h => h.ProcessHours)
                .HasColumnType("decimal(5,2)");

            builder.Entity<HourEntry>()
                .Property(h => h.ManagementHours)
                .HasColumnType("decimal(5,2)");

            builder.Entity<HourEntry>()
                .Property(h => h.RAndDHours)
                .HasColumnType("decimal(5,2)");

            builder.Entity<HourEntry>()
                .Property(h => h.WorkshopHours)
                .HasColumnType("decimal(5,2)");

            builder.Entity<HourEntry>()
                .Property(h => h.OtherHours)
                .HasColumnType("decimal(5,2)");

            builder.Entity<HourEntry>()
                .Property(h => h.TotalHours)
                .HasColumnType("decimal(5,2)");

            builder.Entity<HourEntry>()
                .Property(h => h.InternManagementHours)
                .HasColumnType("decimal(5,2)");
            builder.Entity<HourEntry>()
                .HasIndex(h => h.UserId);

            builder.Entity<HourEntry>()
                .HasIndex(h => new { h.UserId, h.Date });

            // Un seul booking/jour en mode `Daily` (User + Project + Date)
            builder.Entity<HourEntry>()
                .HasIndex(h => new { h.UserId, h.ProjectId, h.Date })
                .IsUnique()
                .HasFilter("[AllocationType] = 0 AND [ProjectId] IS NOT NULL");

            builder.Entity<HourEntry>()
                .HasIndex(h => new { h.UserId, h.Category, h.Date })
                .IsUnique()
                .HasFilter("[AllocationType] = 0 AND [ProjectId] IS NULL");
            builder.Entity<HourEntry>()
                .Property(h => h.HourlyRateAmount)
                .HasColumnType("decimal(10,2)");

            builder.Entity<HourEntry>()
                .Property(h => h.TotalCost)
                .HasColumnType("decimal(10,2)");

            builder.Entity<HourEntry>()
                .HasIndex(h => h.WeekBatchId);

            builder.Entity<HourEntry>()
                .HasIndex(h => new { h.IsPremium, h.PremiumApprovalStatus });
        }

    }
}
