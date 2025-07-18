using Ensek.PeteForrest.Api.Formatters;
using Ensek.PeteForrest.Domain.Repositories;
using Ensek.PeteForrest.Infrastructure;
using Ensek.PeteForrest.Infrastructure.Behaviours;
using Ensek.PeteForrest.Infrastructure.Data;
using Ensek.PeteForrest.Services;
using Ensek.PeteForrest.Services.Model;
using Microsoft.EntityFrameworkCore;

namespace Ensek.PeteForrest.Api;

public class Startup(IConfiguration configuration, IWebHostEnvironment environment)
{
    public IConfiguration Configuration { get; } = configuration;

    public IWebHostEnvironment Environment { get; } = environment;
    
    public void ConfigureServices(IServiceCollection services) {
        services.AddHttpContextAccessor();
        services.AddDbContext<MeterContext>(options =>
            options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

        services.AddInfrastructureServices();
        services.AddServices();

        services.AddControllers(options =>
        {
            options.InputFormatters.Insert(0, new CsvFormatter<MeterReadingLine>(new MeterReadingLineConverter()));
            options.Filters.Add<UnitOfWorkFilter>();
        });
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
    }
    
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseRouting();

        app.UseAuthorization();
        
        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

    }
}