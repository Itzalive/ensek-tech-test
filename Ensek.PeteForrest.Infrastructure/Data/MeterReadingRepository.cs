using Ensek.PeteForrest.Domain;
using Ensek.PeteForrest.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ensek.PeteForrest.Infrastructure.Data;

internal class MeterReadingRepository(MeterContext context) : IMeterReadingRepository
{
    private readonly MeterContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public MeterReading Add(MeterReading meterReading) => _context.MeterReadings.Add(meterReading).Entity;

    public Task<MeterReading[]> GetAsync() => _context.MeterReadings
        .ToArrayAsync();

    public Task<MeterReading?> GetAsync(int id) => _context.MeterReadings
        .Where(a => a.MeterReadingId == id)
        .SingleOrDefaultAsync();
}