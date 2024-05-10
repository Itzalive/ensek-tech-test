using System.Globalization;

using Ensek.PeteForrest.Domain;
using Ensek.PeteForrest.Services.Data;
using Ensek.PeteForrest.Services.Model;

using Microsoft.EntityFrameworkCore;

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

            // Confirm the reading is the newest reading (also confirms it's not a duplicate)
            if (account.MeterReadings?.Any(r => r.DateTime >= dateTime) ?? false)
                return false;

            meterReadingRepository.Add(new MeterReading { Account = account, Value = intValueResult, DateTime = dateTime });
            return true;
        }

        
        public async Task<(int successes, int failures)> TryAddReadingsAsync(IEnumerable<MeterReadingLine> readings)
        {
            var requestedAccountIds = readings.Select(r => r.AccountId).ToArray();
            var validAccounts = await accountRepository.Query.Where(a => requestedAccountIds.Contains(a.AccountId)).ToDictionaryAsync(a => a.AccountId);
            var validAccountIds = validAccounts.Keys.ToArray();

            var latestReadings = await meterReadingRepository.Query
                .Where(r => validAccountIds.Contains(r.Account.AccountId))
                .OrderByDescending(r => r.DateTime)
                .GroupBy(r => r.Account.AccountId)
                .Select(g => g.FirstOrDefault())
                .ToDictionaryAsync(r => r.Account.AccountId);
            
            var successes = 0;
            foreach (var reading in readings)
            {
                if (!validAccountIds.Contains(reading.AccountId)) continue;

                // Parse DateTime
                if (!DateTime.TryParse(reading.MeterReadingDateTime, CultureInfo.CreateSpecificCulture("en-gb"),
                        out var dateTime) && !DateTime.TryParse(reading.MeterReadingDateTime,
                        CultureInfo.InvariantCulture, out dateTime))
                    continue;

                // Parse the reading value
                if (!MeterReading.TryParseValue(reading.MeterReadValue, out var intValueResult))
                    continue;

                // Confirm the reading is the newest reading
                if (latestReadings.ContainsKey(reading.AccountId) &&
                    latestReadings[reading.AccountId].DateTime >= dateTime)
                    continue;

                var account = validAccounts[reading.AccountId];
                var meterReading = new MeterReading {
                    Account = account, Value = intValueResult, DateTime = dateTime
                };
                latestReadings[reading.AccountId] = meterReading;
                meterReadingRepository.Add(meterReading);
                successes++;
            }

            return (successes, readings.Count() - successes);
        }
    }
}
