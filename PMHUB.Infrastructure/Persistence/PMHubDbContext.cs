using Microsoft.EntityFrameworkCore;
using PMHUB.Domain.Entities;

namespace PMHUB.Infrastructure.Persistence
{
    public class PMHubDbContext : DbContext
    {
        public PMHubDbContext(DbContextOptions<PMHubDbContext> options)
            : base(options) { }

        // ── Users ─────────────────────────────────────────────
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Admin> Admins { get; set; } = null!;
        public DbSet<Management> Managements { get; set; } = null!;
        public DbSet<Intern> Interns { get; set; } = null!;

        // ── Core ──────────────────────────────────────────────
        public DbSet<BusinessUnit> BusinessUnits { get; set; } = null!;
        public DbSet<Department> Departments { get; set; } = null!;
        public DbSet<Plant> Plants { get; set; } = null!;
        public DbSet<Project> Projects { get; set; } = null!;

        // ── Sprint & Tasks ────────────────────────────────────
        public DbSet<ProjectTask> Tasks { get; set; } = null!;
        public DbSet<Sprint> Sprints { get; set; } = null!;

        // ── Allocations ───────────────────────────────────────
        public DbSet<HourEntry> HourEntries { get; set; } = null!;
        public DbSet<ProjectAllocation> ProjectAllocations { get; set; } = null!;
        public DbSet<InternAllocation> InternAllocations { get; set; } = null!;
        public DbSet<AllocationTemplate> AllocationTemplates { get; set; } = null!;

        // ── Autres ────────────────────────────────────────────
        public DbSet<KPI> KPIs { get; set; } = null!;
        public DbSet<Report> Reports { get; set; } = null!;
        public DbSet<ProjectFile> ProjectFiles { get; set; } = null!;
        public DbSet<ProjectResource> ProjectResources { get; set; } = null!;
        public DbSet<StrategicCriterion> StrategicCriteria { get; set; } = null!;

        // ── Many-to-Many ──────────────────────────────────────
        public DbSet<ProjectBusinessUnit> ProjectBusinessUnits { get; set; } = null!;
        public DbSet<ProjectTechnology> ProjectTechnologies { get; set; } = null!;
        public DbSet<ProjectSolutionDomain> ProjectSolutionDomains { get; set; } = null!;
        public DbSet<SolutionDomain> SolutionDomains { get; set; } = null!;
        public DbSet<Technology> Technologies { get; set; } = null!;

        // ── Audit ─────────────────────────────────────────────
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ── User ──────────────────────────────────────────
            builder.Entity<User>()
                .HasOne(u => u.ApprovedBy)
                .WithMany()
                .HasForeignKey(u => u.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // ── Intern ────────────────────────────────────────
            builder.Entity<Intern>()
                .HasOne(i => i.Supervisor)
                .WithMany()
                .HasForeignKey(i => i.SupervisorId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── BusinessUnit → Departments ────────────────────
            builder.Entity<BusinessUnit>()
                .HasMany(bu => bu.Departments)
                .WithOne(d => d.BusinessUnit)
                .HasForeignKey(d => d.BusinessUnitId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Plant → Departments ───────────────────────────
            builder.Entity<Plant>()
                .HasMany(p => p.Departments)
                .WithOne(d => d.Plant)
                .HasForeignKey(d => d.PlantId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Department → Projects ─────────────────────────
            builder.Entity<Department>()
                .HasMany(d => d.Projects)
                .WithOne(p => p.Department)
                .HasForeignKey(p => p.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Project self-referencing (sous-projets) ───────
            builder.Entity<Project>()
                .HasOne(p => p.ParentProject)
                .WithMany(p => p.SubProjects)
                .HasForeignKey(p => p.ParentProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            //  Project → Sprints
            builder.Entity<Sprint>()
            .HasOne(s => s.Project)
            .WithMany(p => p.Sprints)
            .HasForeignKey(s => s.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);
            // ── Project → Tasks ───────────────────────────────
            builder.Entity<Project>()
                .HasMany(p => p.Tasks)
                .WithOne(t => t.Project)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            //  Sprint → Tasks (optionnel)
            builder.Entity<ProjectTask>()
                .HasOne(t => t.Sprint)
                .WithMany(s => s.Tasks)
                .HasForeignKey(t => t.SprintId)
                .OnDelete(DeleteBehavior.SetNull);

            // ── Task → AssignedUser ───────────────────────────
            builder.Entity<ProjectTask>()
                .HasOne(t => t.AssignedUser)
                .WithMany()
                .HasForeignKey(t => t.AssignedUserId)
                .OnDelete(DeleteBehavior.SetNull);

            //  Task → HourEntries
            builder.Entity<HourEntry>()
                .HasOne(h => h.Task)
                .WithMany(t => t.HourEntries)
                .HasForeignKey(h => h.TaskId)
                .OnDelete(DeleteBehavior.SetNull);

            // ── Project → Allocations ─────────────────────────
            builder.Entity<Project>()
                .HasMany(p => p.ProjectAllocations)
                .WithOne(pa => pa.Project)
                .HasForeignKey(pa => pa.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Project>()
                .HasMany(p => p.InternAllocations)
                .WithOne(ia => ia.Project)
                .HasForeignKey(ia => ia.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Project → HourEntries ─────────────────────────
            builder.Entity<Project>()
                .HasMany(p => p.HourEntries)
                .WithOne(h => h.Project)
                .HasForeignKey(h => h.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            //  Sprint → HourEntries (optionnel)
            builder.Entity<HourEntry>()
                .HasOne(h => h.Sprint)
                .WithMany()
                .HasForeignKey(h => h.SprintId)
                .OnDelete(DeleteBehavior.SetNull);

            // ── HourEntry → User ──────────────────────────────
            builder.Entity<HourEntry>()
                .HasOne(h => h.User)
                .WithMany(u => u.HourEntries)
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<HourEntry>()
                .HasOne(h => h.InternAllocation)
                .WithMany(ia => ia.HourEntries)
                .HasForeignKey(h => h.InternAllocationId)
                .OnDelete(DeleteBehavior.NoAction);

            // ── ProjectAllocation → User ──────────────────────
            builder.Entity<ProjectAllocation>()
                .HasOne(pa => pa.User)
                .WithMany()
                .HasForeignKey(pa => pa.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Project → autres collections ──────────────────
            builder.Entity<Project>()
                .HasMany(p => p.KPIs)
                .WithOne(k => k.Project)
                .HasForeignKey(k => k.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Project>()
                .HasMany(p => p.ProjectFiles)
                .WithOne(f => f.Project)
                .HasForeignKey(f => f.ProjectId)
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

            // ── Report → User ─────────────────────────────────
            builder.Entity<Report>()
                .HasOne(r => r.CreatedBy)
                .WithMany(u => u.Reports)
                .HasForeignKey(r => r.CreatedById)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Project ↔ User (Members) ──────────────────────
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

            // Index
            builder.Entity<ProjectMember>()
                .HasIndex(pm => new { pm.ProjectId, pm.UserId })
                .IsUnique();

            // ── Many-to-Many : Project ↔ BusinessUnit ─────────
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

            // ── Many-to-Many : Project ↔ Technology ───────────
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

            // ── Many-to-Many : Project ↔ SolutionDomain ───────
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

            // ── Index performances ────────────────────────────
            builder.Entity<Department>()
                .HasIndex(d => d.BusinessUnitId);
            builder.Entity<Department>()
                .HasIndex(d => d.PlantId);
            builder.Entity<Project>()
                .HasIndex(p => p.DepartmentId);
            builder.Entity<Project>()
                .HasIndex(p => p.Status);
            builder.Entity<Project>()
                .HasIndex(p => p.Phase);
            builder.Entity<Sprint>()
                .HasIndex(s => s.ProjectId);
            builder.Entity<ProjectTask>()
                .HasIndex(t => t.ProjectId);
            builder.Entity<ProjectTask>()
                .HasIndex(t => t.SprintId);
            builder.Entity<HourEntry>()
                .HasIndex(h => h.ProjectId);
            builder.Entity<HourEntry>()
                .HasIndex(h => h.TaskId);

            // ── Decimal precision ─────────────────────────────
            builder.Entity<Project>()
                .Property(p => p.Budget)
                .HasColumnType("decimal(18,2)");

            // ── Default values ────────────────────────────────
            builder.Entity<Project>()
                .Property(p => p.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Entity<ProjectResource>()
                .Property(r => r.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        }
    }
}