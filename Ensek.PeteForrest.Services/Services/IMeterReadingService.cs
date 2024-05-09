namespace Ensek.PeteForrest.Services.Services;

public interface IMeterReadingService
{
    public Task<bool> TryAddReadingAsync(int accountId, string value, DateTime dateTime);
}