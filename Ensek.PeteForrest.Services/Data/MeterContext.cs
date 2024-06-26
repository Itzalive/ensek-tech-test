﻿using Ensek.PeteForrest.Domain;
using Microsoft.EntityFrameworkCore;

namespace Ensek.PeteForrest.Services.Data
{
    public class MeterContext : DbContext
    {
        public MeterContext(DbContextOptions<MeterContext> options) : base(options)
        { }

        public DbSet<Account> Accounts { get; init; }

        public DbSet<MeterReading> MeterReadings { get; init; }
    }
}
