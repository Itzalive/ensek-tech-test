using Ensek.PeteForrest.Domain;
using Ensek.PeteForrest.Domain.Repositories;
using Ensek.PeteForrest.Services.Model;
using System.Globalization;
using System.Threading.Channels;

namespace Ensek.PeteForrest.Services.Services
{
    public class MeterReadingService(
        IAccountRepository accountRepository,
        IMeterReadingRepository meterReadingRepository)
        : IMeterReadingService
    {
        private static readonly CultureInfo GbCulture = CultureInfo.CreateSpecificCulture("en-gb");

        public async Task<bool> TryAddReadingAsync(MeterReadingLine reading)
        {
            if (!TryParseMeterReadingAsync(reading, out var parsedMeterReading)) return false;

            var account = await accountRepository.GetAsync(parsedMeterReading.MeterReading.AccountId);
            if (account == null) return false;

            // Confirm the reading is the newest reading (also confirms it's not a duplicate)
            if (account.MeterReadings?.Any(r => r.DateTime >= parsedMeterReading.MeterReading.DateTime) ?? false)
                return false;

            meterReadingRepository.Add(parsedMeterReading.MeterReading);
            account.CurrentMeterReading = parsedMeterReading.MeterReading;
            return true;
        }

        public async Task<(int Successes, int Failures)> TryAddReadingsAsync(
            IAsyncEnumerable<MeterReadingLine> readings)
        {
            const int chunkSize = 500;
            var toBeParsedChannel = Channel.CreateUnbounded<MeterReadingLine>();
            var toBeValidatedChannel = Channel.CreateUnbounded<List<ParsedMeterReading>>();

            var parsingTask = Task.Run(async () =>
            {
                var parsingFailures = 0;
                var parsedMeterReadings = new List<ParsedMeterReading>(chunkSize);
                while (await toBeParsedChannel.Reader.WaitToReadAsync())
                {
                    while (toBeParsedChannel.Reader.TryRead(out var item))
                    {
                        if (TryParseMeterReadingAsync(item, out var parsedMeterReading))
                        {
                            parsedMeterReadings.Add(parsedMeterReading);
                        }
                        else
                        {
                            parsingFailures++;
                        }

                        if (parsedMeterReadings.Count != chunkSize) continue;
                        await toBeValidatedChannel.Writer.WriteAsync(parsedMeterReadings);
                        parsedMeterReadings = new List<ParsedMeterReading>(chunkSize);
                    }
                }

                if (parsedMeterReadings.Count > 0)
                {
                    await toBeValidatedChannel.Writer.WriteAsync(parsedMeterReadings);
                }

                return parsingFailures;
            });

            var validationTask = Task.Run(async () =>
            {
                var failures = 0;
                var successes = 0;
                var accountCache = new Dictionary<int, Account>();
                while (await toBeValidatedChannel.Reader.WaitToReadAsync())
                {
                    while (toBeValidatedChannel.Reader.TryRead(out var parsedMeterReadings))
                    {
                        (failures, successes) =
                            await ValidateAndAddMeterReadings(parsedMeterReadings, accountCache, failures, successes);
                    }
                }

                return (successes, validationFailures: failures);
            });

            await foreach (var line in readings)
            {
                toBeParsedChannel.Writer.TryWrite(line);
            }

            toBeParsedChannel.Writer.Complete();
            var parsingFailures = await parsingTask;
            toBeValidatedChannel.Writer.Complete();
            var (validationSuccesses, validationFailures) = await validationTask;

            return (validationSuccesses, parsingFailures + validationFailures);
        }

        private async Task<(int failures, int successes)> ValidateAndAddMeterReadings(
            List<ParsedMeterReading> parsedMeterReadings, Dictionary<int, Account> accountCache, int failures,
            int successes)
        {
            if (parsedMeterReadings is not { Count: not 0 }) return (failures, successes);
            var newlyRequestedAccountIds = parsedMeterReadings.Select(r => r.MeterReading.AccountId)
                .Distinct().Where(id => !accountCache.ContainsKey(id)).ToList();

            // NOTE: There is a SQL parameter limit on number of account ids that can be passed in at once of 2100,
            // but we've chunked the readings so will not surpass that.
            var newAccounts = await accountRepository.GetAsync(newlyRequestedAccountIds);
            foreach (var account in newAccounts)
            {
                accountCache.Add(account.AccountId, account);
            }

            foreach (var reading in parsedMeterReadings)
            {
                if (!accountCache.TryGetValue(reading.MeterReading.AccountId, out var account))
                {
                    failures++;
                    continue;
                }

                // Confirm the reading is the newest reading
                if (account.CurrentMeterReading != null &&
                    account.CurrentMeterReading.DateTime >= reading.MeterReading.DateTime)
                {
                    failures++;
                    continue;
                }

                meterReadingRepository.Add(reading.MeterReading);
                account.CurrentMeterReading = reading.MeterReading;
                successes++;
            }

            return (failures, successes);
        }

        private static bool TryParseMeterReadingAsync(MeterReadingLine reading,
            out ParsedMeterReading parsedMeterReading)
        {
            if (!reading.AccountId.HasValue)
            {
                parsedMeterReading = null!;
                return false;
            }

            // Parse DateTime
            if (string.IsNullOrEmpty(reading.MeterReadingDateTime) ||
                (!DateTime.TryParse(reading.MeterReadingDateTime, GbCulture,
                    out var dateTime) && !DateTime.TryParse(reading.MeterReadingDateTime, CultureInfo.InvariantCulture,
                    out dateTime)))
            {
                parsedMeterReading = null!;
                return false;
            }

            // Parse the reading value
            if (string.IsNullOrEmpty(reading.MeterReadValue) ||
                !MeterReading.TryParseValue(reading.MeterReadValue, out var intValueResult))
            {
                parsedMeterReading = null!;
                return false;
            }

            parsedMeterReading = new ParsedMeterReading
            {
                RowId = reading.RowId,
                MeterReading = new MeterReading
                {
                    AccountId = reading.AccountId.Value,
                    DateTime = dateTime,
                    Value = intValueResult
                }
            };
            return true;
        }

        internal record ParsedMeterReading
        {
            public required int RowId { get; init; }
            public required MeterReading MeterReading { get; init; }
        }
    }
}