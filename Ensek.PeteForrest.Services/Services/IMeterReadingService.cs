using Ensek.PeteForrest.Services.Model;

namespace Ensek.PeteForrest.Services.Services;

public interface IMeterReadingService
{
    public Task<bool> TryAddReadingAsync(MeterReadingLine reading);

    public Task<(int successes, int failures)> TryAddReadingsAsync(IEnumerable<MeterReadingLine> readings);
}