using Ensek.PeteForrest.Services.Services;
using Ensek.PeteForrest.Services.Services.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace Ensek.PeteForrest.Services
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddScoped<IMeterReadingParser, MeterReadingParser>();
            services.AddScoped<IMeterReadingValidator, MeterReadingMostRecentValidator>();
            services.AddScoped<IMeterReadingService, MeterReadingService>();
            return services;
        }
    }
}