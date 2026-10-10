using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace VALUM.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            var assembly = typeof(Behaviors.ValidationBehavior<,>).Assembly;

            // Register MediatR handlers from this assembly and add the open ValidationBehavior
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(assembly);
                cfg.AddOpenBehavior(typeof(Behaviors.ValidationBehavior<,>));
            });

            // Register FluentValidation validators from this assembly
            services.AddValidatorsFromAssembly(assembly);

            // ValidationBehavior is registered via AddOpenBehavior above

            return services;
        }
    }
}
