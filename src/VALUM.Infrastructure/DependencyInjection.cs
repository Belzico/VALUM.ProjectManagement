using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VALUM.Infrastructure.Persistence;

namespace VALUM.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentException("Connection string must be provided", nameof(connectionString));

            services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

            // Application repositories
            services.AddScoped<VALUM.Application.Projects.IProjectRepository, VALUM.Infrastructure.Projects.ProjectRepository>();
            services.AddScoped<VALUM.Application.Tasks.ITaskRepository, VALUM.Infrastructure.Tasks.TaskRepository>();

            return services;
        }
    }
}
