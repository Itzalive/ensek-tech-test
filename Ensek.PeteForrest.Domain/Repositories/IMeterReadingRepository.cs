using Ensek.PeteForrest.Domain;

namespace Ensek.PeteForrest.Domain.Repositories;

public interface IMeterReadingRepository
{
    MeterReading Add(MeterReading meterReading);
    
    Task<MeterReading[]> GetAsync();

    Task<MeterReading?> GetAsync(int id);
}