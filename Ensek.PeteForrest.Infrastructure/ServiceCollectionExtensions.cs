using Ensek.PeteForrest.Domain.Repositories;
using Ensek.PeteForrest.Infrastructure.Behaviours;
using Ensek.PeteForrest.Infrastructure.Data;
using Ensek.PeteForrest.Services.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Ensek.PeteForrest.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static void AddInfrastructureServices(this IServiceCollection services)
        {
            services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();

            // Register repositories
            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<IMeterReadingRepository, MeterReadingRepository>();
        }
    }
}
