using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace VALUM.Infrastructure.Persistence
{
    public static class DatabaseInitializer
    {
        public static async Task ApplyMigrationsAsync(IServiceProvider services, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(services);

            await using var scope = services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Propagate exceptions so startup can fail visibly if migrations cannot be applied
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
