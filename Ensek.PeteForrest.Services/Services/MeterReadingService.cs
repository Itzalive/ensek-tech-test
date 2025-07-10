using Ensek.PeteForrest.Domain;
using Ensek.PeteForrest.Domain.Repositories;
using Ensek.PeteForrest.Services.Model;
using System.Threading.Channels;

namespace Ensek.PeteForrest.Services.Services
{
    public class MeterReadingService(
        IAccountRepository accountRepository,
        IMeterReadingRepository meterReadingRepository,
        IMeterReadingParser meterReadingParser,
        IEnumerable<IMeterReadingValidator> meterReadingValidators)
        : IMeterReadingService
    {
        public async Task<bool> TryAddReadingAsync(MeterReadingLine reading)
        {
            if (!meterReadingParser.TryParse(reading, out var parsedMeterReading)) return false;

            var account = await accountRepository.GetAsync(parsedMeterReading.MeterReading.AccountId);
            if (account == null) return false;

            var validationFailed = false;
            using var cancellationTokenSource = new CancellationTokenSource();
            try
            {
                await Parallel.ForEachAsync(meterReadingValidators, cancellationTokenSource.Token,
                    async (meterReadingValidator, ct) =>
                    {
                        var validationResult =
                            await meterReadingValidator.ValidateAsync(parsedMeterReading.MeterReading, account, ct);
                        if (validationResult.IsValid) return;
                        await cancellationTokenSource.CancelAsync();
                        validationFailed = true;
                    });
            }
            catch (OperationCanceledException)
            {
                // If the task was cancelled, we assume validation failed
                return false;
            }

            if (validationFailed)
                return false;

            meterReadingRepository.Add(parsedMeterReading.MeterReading);
            account.CurrentMeterReading = parsedMeterReading.MeterReading;
            return true;
        }

        public async Task<(int Successes, int Failures)> TryAddReadingsAsync(
            IAsyncEnumerable<MeterReadingLine> readings, CancellationToken cancellationToken = default)
        {
            const int chunkSize = 500;
            var channelOptions = new BoundedChannelOptions(chunkSize)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = true
            };

            // Create two channels: one for parsing and one for validation,
            // this allows decoupling the parsing and validation steps
            // and to begin parsing as soon as we have streamed the first readings
            var toBeParsedChannel = Channel.CreateBounded<MeterReadingLine>(channelOptions);
            var toBeValidatedChannel = Channel.CreateBounded<List<ParsedMeterReading>>(channelOptions);

            var parsingTask = Task.Run(async () =>
            {
                var parsingFailures = 0;
                var parsedMeterReadings = new List<ParsedMeterReading>(chunkSize);
                while (await toBeParsedChannel.Reader.WaitToReadAsync(cancellationToken))
                {
                    while (toBeParsedChannel.Reader.TryRead(out var item))
                    {
                        if (meterReadingParser.TryParse(item, out var parsedMeterReading))
                        {
                            parsedMeterReadings.Add(parsedMeterReading);
                        }
                        else
                        {
                            parsingFailures++;
                        }

                        if (parsedMeterReadings.Count != chunkSize) continue;
                        await toBeValidatedChannel.Writer.WriteAsync(parsedMeterReadings, cancellationToken);
                        parsedMeterReadings = new List<ParsedMeterReading>(chunkSize);
                    }
                }

                if (parsedMeterReadings.Count > 0)
                {
                    await toBeValidatedChannel.Writer.WriteAsync(parsedMeterReadings, cancellationToken);
                }

                return parsingFailures;
            }, cancellationToken);

            var validationTask = Task.Run(async () =>
            {
                var failures = 0;
                var successes = 0;
                var accountCache = new Dictionary<int, Account>();
                while (await toBeValidatedChannel.Reader.WaitToReadAsync(cancellationToken))
                {
                    while (toBeValidatedChannel.Reader.TryRead(out var parsedMeterReadings))
                    {
                        (failures, successes) =
                            await ValidateAndAddMeterReadings(parsedMeterReadings, accountCache, failures, successes);
                    }
                }

                return (successes, validationFailures: failures);
            }, cancellationToken);

            await foreach (var line in readings.WithCancellation(cancellationToken))
            {
                await toBeParsedChannel.Writer.WriteAsync(line, cancellationToken);
            }

            toBeParsedChannel.Writer.Complete();
            var parsingFailures = await parsingTask.WaitAsync(cancellationToken);
            toBeValidatedChannel.Writer.Complete();
            var (validationSuccesses, validationFailures) = await validationTask.WaitAsync(cancellationToken);

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

                var validationFailed = false;
                using var cancellationTokenSource = new CancellationTokenSource();
                try
                {
                    await Parallel.ForEachAsync(meterReadingValidators, cancellationTokenSource.Token,
                        async (meterReadingValidator, ct) =>
                        {
                            var validationResult =
                                await meterReadingValidator.ValidateAsync(reading.MeterReading, account, ct);
                            if (validationResult.IsValid) return;
                            await cancellationTokenSource.CancelAsync();
                            validationFailed = true;
                        });
                }
                catch (OperationCanceledException)
                {
                    // If the task was cancelled, we assume validation failed
                    failures++;
                    continue;
                }

                if (validationFailed)
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
    }
}