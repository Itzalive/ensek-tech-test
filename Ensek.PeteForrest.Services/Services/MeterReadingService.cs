using Ensek.PeteForrest.Domain;
using Ensek.PeteForrest.Domain.Repositories;
using Ensek.PeteForrest.Services.Model;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace Ensek.PeteForrest.Services.Services
{
    public class MeterReadingService(
        IAccountRepository accountRepository,
        IMeterReadingRepository meterReadingRepository,
        IMeterReadingParser meterReadingParser,
        IEnumerable<IMeterReadingValidator> meterReadingValidators,
        ILogger<MeterReadingService> logger)
        : IMeterReadingService
    {
        public async Task<bool> TryAddReadingAsync(MeterReadingLine reading)
        {
            logger.LogDebug("Processing meter reading for account {AccountId}", reading.AccountId);

            if (reading.ParseErrors != ParseErrors.None)
            {
                logger.LogWarning("Failed to parse meter reading on row {RowId}", reading.RowId);
            }

            if (!meterReadingParser.TryParse(reading, out var parsedMeterReading))
            {
                logger.LogWarning("Failed to parse meter reading {@Reading}", reading);
                return false;
            }

            var account = await accountRepository.GetAsync(parsedMeterReading.MeterReading.AccountId);
            if (account == null)
            {
                logger.LogWarning("Account {AccountId} not found for reading on row {RowId}",
                    parsedMeterReading.MeterReading.AccountId, parsedMeterReading.RowId);
                return false;
            }

            if (!await ValidateReadingAsync(parsedMeterReading, account)) return false;

            meterReadingRepository.Add(parsedMeterReading.MeterReading);
            account.CurrentMeterReading = parsedMeterReading.MeterReading;
            logger.LogDebug("Successfully added meter reading from row {RowId} for account {AccountId}",
                reading.RowId, account.AccountId);
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
                            logger.LogWarning("Failed to parse meter reading {@Reading}", item);
                            parsingFailures++;
                        }

                        if (parsedMeterReadings.Count != chunkSize) continue;
                        logger.LogDebug("Sending chunk of {Count} readings for validation", parsedMeterReadings.Count);
                        await toBeValidatedChannel.Writer.WriteAsync(parsedMeterReadings, cancellationToken);
                        parsedMeterReadings = new List<ParsedMeterReading>(chunkSize);
                    }
                }

                if (parsedMeterReadings.Count > 0)
                {
                    logger.LogDebug("Sending final chunk of {Count} readings for validation",
                        parsedMeterReadings.Count);
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

            logger.LogDebug("Starting batch processing of meter readings with chunk size {ChunkSize}", chunkSize);

            await foreach (var line in readings.WithCancellation(cancellationToken))
            {
                if (line.ParseErrors != ParseErrors.None)
                {
                    logger.LogWarning("Failed to parse meter reading on row {RowId}", line.RowId);
                    continue;
                }

                await toBeParsedChannel.Writer.WriteAsync(line, cancellationToken);
            }

            toBeParsedChannel.Writer.Complete();
            var parsingFailures = await parsingTask.WaitAsync(cancellationToken);
            toBeValidatedChannel.Writer.Complete();
            var (validationSuccesses, validationFailures) = await validationTask.WaitAsync(cancellationToken);
            logger.LogInformation(
                "Batch processing completed. Successes: {SuccessCount}, Parsing Failures: {ParsingFailures}, Validation Failures: {ValidationFailures}",
                validationSuccesses, parsingFailures, validationFailures);

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
                    logger.LogWarning("Account {AccountId} not found for reading on row {RowId}",
                        reading.MeterReading.AccountId, reading.RowId);
                    failures++;
                    continue;
                }

                if (!await ValidateReadingAsync(reading, account))
                {
                    failures++;
                    continue;
                }

                meterReadingRepository.Add(reading.MeterReading);
                account.CurrentMeterReading = reading.MeterReading;
                logger.LogDebug("Successfully added meter reading from row {RowId} for account {AccountId}",
                    reading.RowId, account.AccountId);
                successes++;
            }

            return (failures, successes);
        }

        private async Task<bool> ValidateReadingAsync(ParsedMeterReading parsedMeterReading, Account account)
        {
            foreach (var validationRule in meterReadingValidators)
            {
                var validationResult =
                    await validationRule.ValidateAsync(parsedMeterReading.MeterReading, account, ct);
                if (validationResult.IsValid) continue;
                logger.LogWarning("Validation failed for reading on row {RowId}: {ValidationError}",
                    parsedMeterReading.RowId, validationResult.Error);
                return false;
            }

            return true;
        }
    }
}