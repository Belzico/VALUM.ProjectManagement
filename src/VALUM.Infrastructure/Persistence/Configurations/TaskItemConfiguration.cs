using System;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VALUM.Domain.Entities;

namespace VALUM.Infrastructure.Persistence.Configurations
{
    public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
    {
        public void Configure(EntityTypeBuilder<TaskItem> builder)
        {
            builder.ToTable("Tasks");

            builder.HasKey(t => t.Id);
            builder.Property(t => t.Id).ValueGeneratedNever();

            builder.Property(t => t.Title)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(t => t.Description)
                .IsRequired(false);

            builder.Property(t => t.Priority).IsRequired();
            builder.Property(t => t.Status).IsRequired();

            // DateTime converters for SQLite round-trip preserving DateTimeKind.Utc
            var dtConverter = new ValueConverter<DateTime, string>(
                v => v.ToString("o"),
                v => DateTime.Parse(v, null, DateTimeStyles.RoundtripKind));

            var nullableDtConverter = new ValueConverter<DateTime?, string?>(
                v => v.HasValue ? v.Value.ToString("o") : null,
                v => v == null ? (DateTime?)null : DateTime.Parse(v, null, DateTimeStyles.RoundtripKind));

            builder.Property(t => t.CreatedAt)
                .HasConversion(dtConverter)
                .IsRequired();

            builder.Property(t => t.DueDate)
                .HasConversion(nullableDtConverter)
                .IsRequired(false);

            builder.Property(t => t.CompletedAt)
                .HasConversion(nullableDtConverter)
                .IsRequired(false);

            builder.Property(t => t.IsDeleted)
                .HasDefaultValue(false)
                .IsRequired();

            // Foreign key to Project (no navigation properties in domain)
            builder.HasOne<VALUM.Domain.Entities.Project>()
                .WithMany()
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        }
    }
}
