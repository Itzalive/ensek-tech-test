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

            MeterReading meterReading = new MeterReading { Account = account, Value = intValueResult, DateTime = dateTime };
            meterReadingRepository.Add(meterReading);
            account.CurrentReading = meterReading;
            return true;
        }

        
        public async Task<(int successes, int failures)> TryAddReadingsAsync(IEnumerable<MeterReadingLine> readings)
        {
            var requestedAccountIds = readings.Select(r => r.AccountId).Distinct().ToArray();

            // NOTE: There is a SQL parameter limit on number of account ids that can be passed in at once of 2100.
            var requestedAccounts = new List<Account>(requestedAccountIds.Length);
            foreach (var requestedAccountIdChunk in requestedAccountIds.Chunk(2000)) {
                requestedAccounts.AddRange(await accountRepository.Query.Include(a => a.CurrentReading)
                    .Where(a => requestedAccountIdChunk.Contains(a.AccountId))
                    .ToArrayAsync());
            }

            var validAccounts = requestedAccounts.ToDictionary(a => a.AccountId);

            var successes = 0;
            foreach (var reading in readings)
            {
                if (!validAccounts.ContainsKey(reading.AccountId)) continue;

                // Parse DateTime
                if (!DateTime.TryParse(reading.MeterReadingDateTime, CultureInfo.CreateSpecificCulture("en-gb"),
                        out var dateTime) && !DateTime.TryParse(reading.MeterReadingDateTime,
                        CultureInfo.InvariantCulture, out dateTime))
                    continue;

                // Parse the reading value
                if (!MeterReading.TryParseValue(reading.MeterReadValue, out var intValueResult))
                    continue;

                var account = validAccounts[reading.AccountId];

                // Confirm the reading is the newest reading
                if (account.CurrentReading != null && account.CurrentReading.DateTime >= dateTime)
                    continue;

                var meterReading = new MeterReading {
                    Account = account, Value = intValueResult, DateTime = dateTime
                };
                meterReadingRepository.Add(meterReading);
                account.CurrentReading = meterReading;
                successes++;
            }

            return (successes, readings.Count() - successes);
        }
    }
}
