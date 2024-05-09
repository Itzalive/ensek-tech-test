using System.Globalization;
using CsvHelper;
using Ensek.PeteForrest.Domain;
using Ensek.PeteForrest.Services.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Ensek.PeteForrest.Api;
public class Program {
    public static void Main(string[] args) {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
}

public class Startup(IConfiguration configuration, IWebHostEnvironment environment)
{
    public IConfiguration Configuration { get; } = configuration;

    public IWebHostEnvironment Environment { get; } = environment;
    
    public void ConfigureServices(IServiceCollection services) {
        services.AddDbContext<MeterContext>(options =>
            options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));
        services.AddScoped<IAccountRepository, AccountRepository>();

        services.AddControllers();
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



public static class DbInitializer
{
    public static void Initialize(MeterContext context)
    {
        context.Database.EnsureCreated();

        if (context.Accounts.Any()) return;

        
        using var transaction = context.Database.BeginTransaction();
        try
        {
            context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT dbo.Accounts ON");
            context.SaveChanges();

            InsertAccountsFromCsv(context, "Data/Test_Accounts 2.csv");

            transaction.Commit();
        }
        catch(Exception)
        {
            transaction.Rollback();
            throw;
        }
        finally
        {
            context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT dbo.Accounts OFF");
            context.SaveChanges();
        }
    }

    public static void InsertAccountsFromCsv(MeterContext context, string path)
    {
        using (var reader = new StreamReader(path))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            var records = csv.GetRecords<Account>();
            foreach (var record in records)
            {
                context.Database.ExecuteSql(
                    $"INSERT INTO Accounts (AccountId, FirstName, LastName) values ({record.AccountId}, {record.FirstName}, {record.LastName});");
            }
        }
        context.SaveChanges();
    }
}