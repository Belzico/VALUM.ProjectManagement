using System;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VALUM.Domain.Entities;

namespace VALUM.Infrastructure.Persistence.Configurations
{
    public class ProjectConfiguration : IEntityTypeConfiguration<Project>
    {
        public void Configure(EntityTypeBuilder<Project> builder)
        {
            builder.ToTable("Projects");

            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id).ValueGeneratedNever();

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(80);

            builder.Property(p => p.Description)
                .IsRequired(false);

            builder.Property(p => p.Status)
                .IsRequired(); // enum as int

            // UTC DateTime converter stored as round-trip string
            var dateTimeConverter = new ValueConverter<DateTime, string>(
                v => v.ToString("o"),
                v => DateTime.Parse(v, null, DateTimeStyles.RoundtripKind));

            builder.Property(p => p.CreatedAt)
                .HasConversion(dateTimeConverter)
                .IsRequired();

            builder.Property(p => p.OwnerId)
                .IsRequired();
        }
    }
}
