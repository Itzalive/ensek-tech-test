using Ensek.PeteForrest.Domain;
using Microsoft.EntityFrameworkCore;

namespace Ensek.PeteForrest.Infrastructure.Data
{
    public class MeterContext(DbContextOptions<MeterContext> options) : DbContext(options)
    {
        public DbSet<Account> Accounts { get; init; }

        public DbSet<MeterReading> MeterReadings { get; init; }
    }
}
