using Ensek.PeteForrest.Domain.Repositories;
using Ensek.PeteForrest.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Ensek.PeteForrest.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static void AddInfrastructureServices(this IServiceCollection services)
        {
            // Register repositories
            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<IMeterReadingRepository, MeterReadingRepository>();
        }
    }
}
