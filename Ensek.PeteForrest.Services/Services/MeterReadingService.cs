using Ensek.PeteForrest.Domain;
using Ensek.PeteForrest.Services.Data;

namespace Ensek.PeteForrest.Services.Services
{
    public class MeterReadingService(
        IAccountRepository accountRepository,
        IMeterReadingRepository meterReadingRepository)
    : IMeterReadingService
    {
        public async Task<bool> TryAddReadingAsync(int accountId, string value, DateTime dateTime)
        {
            var account = await accountRepository.GetAsync(accountId);
            if (account == null) return false;

            // Parse the reading value
            if (!MeterReading.TryParseValue(value, out var intValueResult))
                return false;

            // Check for duplicates
            if (account.MeterReadings?.Any(r => r.Value == intValueResult && r.DateTime == dateTime) ?? false)
                return false;

            // Confirm the reading is the newest reading
            if (account.MeterReadings?.Any(r => r.DateTime > dateTime) ?? false)
                return false;

            meterReadingRepository.Add(new MeterReading { Account = account, Value = intValueResult, DateTime = dateTime });
            return true;
        }
    }
}
