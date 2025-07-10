using System.Globalization;

using Ensek.PeteForrest.Domain;
using Ensek.PeteForrest.Services.Data;
using Ensek.PeteForrest.Domain.Repositories;
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

        
        public async Task<(int successes, int failures)> TryAddReadingsAsync(IEnumerable<MeterReadingLine> readings) {
            var successes = 0;
            var failures = 0;
            var seenAccounts = new Dictionary<int, Account>();
            foreach (var readingChunk in readings.Chunk(2000)) {
                var newlyRequestedAccountIds = readingChunk.Select(r => r.AccountId).Distinct().Where(id => !seenAccounts.ContainsKey(id)).ToList();

                // NOTE: There is a SQL parameter limit on number of account ids that can be passed in at once of 2100,
                // but we've chunked the readings so will not surpass that.
                var newAccounts = await accountRepository.GetAsync(newlyRequestedAccountIds);
                foreach(var account in newAccounts) {
                    seenAccounts.Add(account.AccountId, account);
                }

                foreach (var reading in readings) {
                    if (!seenAccounts.ContainsKey(reading.AccountId)) {
                        failures++;
                        continue;
                    }

                    // Parse DateTime
                    if (!DateTime.TryParse(reading.MeterReadingDateTime, CultureInfo.CreateSpecificCulture("en-gb"),
                            out var dateTime) && !DateTime.TryParse(reading.MeterReadingDateTime,
                            CultureInfo.InvariantCulture, out dateTime)) {
                        failures++;
                        continue;
                    }

                    // Parse the reading value
                    if (!MeterReading.TryParseValue(reading.MeterReadValue, out var intValueResult)) {
                        failures++;
                        continue;
                    }

                    var account = seenAccounts[reading.AccountId];

                    // Confirm the reading is the newest reading
                    if (account.CurrentReading != null && account.CurrentReading.DateTime >= dateTime) {
                        failures++;
                        continue;
                    }

                    var meterReading = new MeterReading {
                        Account = account,
                        Value = intValueResult,
                        DateTime = dateTime
                    };
                    meterReadingRepository.Add(meterReading);
                    account.CurrentReading = meterReading;
                    successes++;
                }
            }
            return (successes, failures);
        }
    }
}
