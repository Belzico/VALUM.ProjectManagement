using Microsoft.EntityFrameworkCore;
using VALUM.Domain.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VALUM.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Project> Projects { get; set; } = null!;
        public DbSet<TaskItem> Tasks { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }

        private void ValidateUtcDateTimes()
        {
            // Validate Project.CreatedAt
            var projectEntries = ChangeTracker.Entries<Project>()
                .Where(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Added || e.State == Microsoft.EntityFrameworkCore.EntityState.Modified);

            foreach (var e in projectEntries)
            {
                var dt = e.Entity.CreatedAt;
                if (dt.Kind != DateTimeKind.Utc)
                {
                    throw new ArgumentException("ProjectCreatedAt must be UTC");
                }
            }

            // Validate TaskItem date fields
            var taskEntries = ChangeTracker.Entries<TaskItem>()
                .Where(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Added || e.State == Microsoft.EntityFrameworkCore.EntityState.Modified);

            foreach (var e in taskEntries)
            {
                var ent = e.Entity;

                if (ent.CreatedAt.Kind != DateTimeKind.Utc)
                {
                    throw new ArgumentException("TaskCreatedAt must be UTC");
                }

                if (ent.DueDate.HasValue && ent.DueDate.Value.Kind != DateTimeKind.Utc)
                {
                    throw new ArgumentException("TaskDueDate must be UTC");
                }

                if (ent.CompletedAt.HasValue && ent.CompletedAt.Value.Kind != DateTimeKind.Utc)
                {
                    throw new ArgumentException("TaskCompletedAt must be UTC");
                }
            }
        }

        public override int SaveChanges()
        {
            ValidateUtcDateTimes();
            return base.SaveChanges();
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            ValidateUtcDateTimes();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ValidateUtcDateTimes();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            ValidateUtcDateTimes();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
    }
}
