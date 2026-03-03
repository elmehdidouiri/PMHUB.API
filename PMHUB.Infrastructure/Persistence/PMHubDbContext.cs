using Microsoft.EntityFrameworkCore;
using PMHUB.Domain.Entities;

namespace PMHUB.Infrastructure.Persistence
{
    public class PMHubDbContext : DbContext
    {
        public PMHubDbContext(DbContextOptions<PMHubDbContext> options)
            : base(options) { }

         public DbSet<User> Users { get; set; } = null!;
        public DbSet<Admin> Admins { get; set; } = null!;
        public DbSet<Management> Managements { get; set; } = null!;
        public DbSet<Intern> Interns { get; set; } = null!;

         public DbSet<BusinessUnit> BusinessUnits { get; set; } = null!;
        public DbSet<Department> Departments { get; set; } = null!;
        public DbSet<Plant> Plants { get; set; } = null!;
        public DbSet<Project> Projects { get; set; } = null!;

         public DbSet<ProjectTask> Tasks { get; set; } = null!;
        public DbSet<Sprint> Sprints { get; set; } = null!;

         public DbSet<HourEntry> HourEntries { get; set; } = null!;
        public DbSet<ProjectAllocation> ProjectAllocations { get; set; } = null!;
        public DbSet<InternAllocation> InternAllocations { get; set; } = null!;
        public DbSet<AllocationTemplate> AllocationTemplates { get; set; } = null!;

         public DbSet<KPI> KPIs { get; set; } = null!;
        public DbSet<Report> Reports { get; set; } = null!;
        public DbSet<ProjectFile> ProjectFiles { get; set; } = null!;
        public DbSet<ProjectResource> ProjectResources { get; set; } = null!;
        public DbSet<StrategicCriterion> StrategicCriteria { get; set; } = null!;

         public DbSet<ProjectBusinessUnit> ProjectBusinessUnits { get; set; } = null!;
        public DbSet<ProjectTechnology> ProjectTechnologies { get; set; } = null!;
        public DbSet<ProjectSolutionDomain> ProjectSolutionDomains { get; set; } = null!;
        public DbSet<SolutionDomain> SolutionDomains { get; set; } = null!;
        public DbSet<Technology> Technologies { get; set; } = null!;

         public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

             builder.Entity<User>()
                .HasOne(u => u.ApprovedBy)
                .WithMany()
                .HasForeignKey(u => u.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

             builder.Entity<Intern>()
                .HasOne(i => i.Supervisor)
                .WithMany()
                .HasForeignKey(i => i.SupervisorId)
                .OnDelete(DeleteBehavior.Restrict);

             builder.Entity<BusinessUnit>()
                .HasMany(bu => bu.Departments)
                .WithOne(d => d.BusinessUnit)
                .HasForeignKey(d => d.BusinessUnitId)
                .OnDelete(DeleteBehavior.Restrict);

             builder.Entity<Plant>()
                .HasMany(p => p.Departments)
                .WithOne(d => d.Plant)
                .HasForeignKey(d => d.PlantId)
                .OnDelete(DeleteBehavior.Restrict);

             builder.Entity<Department>()
                .HasMany(d => d.Projects)
                .WithOne(p => p.Department)
                .HasForeignKey(p => p.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

             builder.Entity<Project>()
                .HasOne(p => p.ParentProject)
                .WithMany(p => p.SubProjects)
                .HasForeignKey(p => p.ParentProjectId)
                .OnDelete(DeleteBehavior.Restrict);

             builder.Entity<Project>()
                .HasMany(p => p.Tasks)
                .WithOne(t => t.Project)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

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

             builder.Entity<Project>()
                .HasMany(p => p.HourEntries)
                .WithOne(h => h.Project)
                .HasForeignKey(h => h.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

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

             builder.Entity<Project>()
                .HasMany(p => p.Members)
                .WithMany(u => u.Projects)
                .UsingEntity(j => j.ToTable("ProjectMembers"));

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

             builder.Entity<ProjectTask>()
                .HasOne(t => t.AssignedUser)
                .WithMany()
                .HasForeignKey(t => t.AssignedUserId)
                .OnDelete(DeleteBehavior.SetNull);

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

             builder.Entity<ProjectAllocation>()
                .HasOne(pa => pa.User)
                .WithMany()
                .HasForeignKey(pa => pa.UserId)
                .OnDelete(DeleteBehavior.Restrict);

             builder.Entity<Report>()
                .HasOne(r => r.CreatedBy)
                .WithMany(u => u.Reports)
                .HasForeignKey(r => r.CreatedById)
                .OnDelete(DeleteBehavior.Cascade);

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

             builder.Entity<Project>()
                .Property(p => p.Budget)
                .HasColumnType("decimal(18,2)");

             builder.Entity<Project>()
                .Property(p => p.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Entity<ProjectResource>()
                .Property(r => r.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        }
    }
}