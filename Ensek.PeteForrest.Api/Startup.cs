using Ensek.PeteForrest.Api.Formatters;
using Ensek.PeteForrest.Domain.Repositories;
using Ensek.PeteForrest.Infrastructure.Behaviours;
using Ensek.PeteForrest.Infrastructure.Data;
using Ensek.PeteForrest.Services.Model;
using Ensek.PeteForrest.Services.Services;
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
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IMeterReadingRepository, MeterReadingRepository>();
        services.AddScoped<IMeterReadingService, MeterReadingService>();

        services.AddControllers(options =>
        {
            options.InputFormatters.Insert(0, new CsvFormatter<MeterReadingLine>(new MeterReadingLineConverter()));
            options.Filters.Add<UnitOfWorkFilter>();
        });
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
    }
    
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
        CreateDbIfNotExists(app);

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

    private static void CreateDbIfNotExists(IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<MeterContext>();
            DbInitializer.Initialize(context);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred creating the DB.");
        }
    }
}