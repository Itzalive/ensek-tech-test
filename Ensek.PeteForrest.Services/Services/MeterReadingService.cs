using System.Globalization;

using Ensek.PeteForrest.Domain;
using Ensek.PeteForrest.Services.Data;
using Ensek.PeteForrest.Services.Model;

namespace Ensek.PeteForrest.Services.Services
{
    public class MeterReadingService(
        IAccountRepository accountRepository,
        IMeterReadingRepository meterReadingRepository)
    : IMeterReadingService
    {
        public async Task<bool> TryAddReadingAsync(MeterReadingLine reading)
        {
            var account = await accountRepository.GetAsync(reading.AccountId);
            if (account == null) return false;

            // Parse DateTime
            if (!DateTime.TryParse(reading.MeterReadingDateTime, CultureInfo.CreateSpecificCulture("en-gb"), out var dateTime) && !DateTime.TryParse(reading.MeterReadingDateTime, CultureInfo.InvariantCulture, out dateTime))
                return false;

            // Parse the reading value
            if (!MeterReading.TryParseValue(reading.MeterReadValue, out var intValueResult))
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
