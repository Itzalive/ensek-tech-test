using Ensek.PeteForrest.Domain;
using Ensek.PeteForrest.Domain.Repositories;
using Ensek.PeteForrest.Services.Infrastructure;
using Ensek.PeteForrest.Services.Model;
using Ensek.PeteForrest.Services.Models;
using Microsoft.Extensions.Logging;

namespace Ensek.PeteForrest.Services.Services.Implementations
{
    internal class MeterReadingService(
        IAccountRepository accountRepository,
        IMeterReadingRepository meterReadingRepository,
        IMeterReadingParser meterReadingParser,
        IUnitOfWorkFactory unitOfWorkFactory,
        IEnumerable<IMeterReadingValidator> meterReadingValidators,
        ILogger<IMeterReadingService> logger)
        : IMeterReadingService
    {
        public async Task<bool> TryAddReadingAsync(MeterReadingLine reading)
        {
            logger.LogDebug("Processing meter reading for account {AccountId}", reading.AccountId);

            if (reading.ParseErrors != ParseErrors.None)
            {
                logger.LogWarning("Failed to parse meter reading on row {RowId}", reading.RowId);
                return false;
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
            const int chunkSize = 2000;
            var successes = 0;
            var failures = 0;
            await foreach (var readingChunk in readings.Chunk(chunkSize).WithCancellation(cancellationToken))
            {
                var parsedReadings = new List<ParsedMeterReading>(chunkSize);
                foreach (var reading in readingChunk)
                {
                    if (reading.ParseErrors != ParseErrors.None)
                    {
                        logger.LogWarning("Failed to parse meter reading on row {RowId}", reading.RowId);
                        failures++;
                        continue;
                    }

                    if (!meterReadingParser.TryParse(reading, out var parsedMeterReading))
                    {
                        logger.LogWarning("Failed to parse meter reading {@Reading}", reading);
                        failures++;
                        continue;
                    }

                    parsedReadings.Add(parsedMeterReading);
                }

                await using var unitOfWork = unitOfWorkFactory.Create();
                try
                {
                    var (newSuccesses, newFailures) = await ValidateAndAddMeterReadings(parsedReadings);
                    failures += newFailures;
                    successes += newSuccesses;
                }
                catch
                {
                    await unitOfWork.RollbackAsync();
                    failures += parsedReadings.Count;
                }
            }

            return (successes, failures);
        }

        private async Task<(int Successes, int Failures)> ValidateAndAddMeterReadings(
            List<ParsedMeterReading> parsedMeterReadings)
        {
            var successes = 0;
            var failures = 0;
            if (parsedMeterReadings is not { Count: not 0 }) return (0, 0);
            var newlyRequestedAccountIds = parsedMeterReadings.Select(r => r.MeterReading.AccountId).Distinct().ToList();

            // NOTE: There is a SQL parameter limit on number of account ids that can be passed in at once of 2100,
            // but we've chunked the readings so will not surpass that.
            var accounts = await accountRepository.GetAsync(newlyRequestedAccountIds);
            var accountLookup = accounts.ToDictionary(a => a.AccountId);

            foreach (var reading in parsedMeterReadings)
            {
                if (!accountLookup.TryGetValue(reading.MeterReading.AccountId, out var account))
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
            return (successes, failures);
        }

        private async Task<bool> ValidateReadingAsync(ParsedMeterReading parsedMeterReading, Account account)
        {
            foreach (var validationRule in meterReadingValidators)
            {
                var validationResult =
                    await validationRule.ValidateAsync(parsedMeterReading.MeterReading, account);
                if (validationResult.IsValid) continue;
                logger.LogWarning("Validation failed for reading on row {RowId}: {ValidationError}",
                    parsedMeterReading.RowId, validationResult.Error);
                return false;
            }

            return true;
        }
    }
}