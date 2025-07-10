using Ensek.PeteForrest.Services.Model;

namespace Ensek.PeteForrest.Services.Services;

public interface IMeterReadingService
{
    public Task<bool> TryAddReadingAsync(MeterReadingLine reading);

    public Task<(int Successes, int Failures)> TryAddReadingsAsync(IAsyncEnumerable<MeterReadingLine> readings,
        CancellationToken cancellationToken = default);
}