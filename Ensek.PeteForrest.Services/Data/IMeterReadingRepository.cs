using Ensek.PeteForrest.Domain;

namespace Ensek.PeteForrest.Services.Data;

public interface IMeterReadingRepository
{
    MeterReading Add(MeterReading meterReading);
    
    Task<MeterReading[]> GetAsync();

    Task<MeterReading?> GetAsync(int id);

    IQueryable<MeterReading> Query { get; }
}