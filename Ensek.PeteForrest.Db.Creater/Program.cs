using Ensek.PeteForrest.Api;
using Ensek.PeteForrest.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("Connecting to DB...");

var builder = new ConfigurationBuilder()
.SetBasePath(AppContext.BaseDirectory)
.AddJsonFile("appsettings.json", optional: false)
.AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true)
.AddEnvironmentVariables();

IConfiguration config = builder.Build();

var services = new ServiceCollection();
services.AddDbContext<MeterContext>(options =>
    options.UseSqlServer(config.GetConnectionString("DefaultConnection")));

var serviceProvider = services.BuildServiceProvider();

var context = serviceProvider.GetRequiredService<MeterContext>();

Console.WriteLine("Ensuring DB schema is up to date...");

await context.Database.EnsureCreatedAsync();

Console.WriteLine("Seeding starting data...");

var seededAccounts = await AccountSeeder.SeedAccountsAsync(context, "Data/Test_Accounts 2.csv");

if (seededAccounts) {
    Console.WriteLine("Seeded Account data");
}

Console.WriteLine("done");
