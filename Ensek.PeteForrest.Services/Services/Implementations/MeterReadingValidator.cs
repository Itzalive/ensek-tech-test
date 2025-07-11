using Ensek.PeteForrest.Domain;

namespace Ensek.PeteForrest.Services.Services;

public class MeterReadingMostRecentValidator : IMeterReadingValidator
{
    public Task<ValidationResult> ValidateAsync(MeterReading reading, Account account, CancellationToken cancellationToken = default)
    {
        // Confirm the reading is the newest reading
        if (account.CurrentMeterReading != null &&
            account.CurrentMeterReading.DateTime >= reading.DateTime)
        {
            return Task.FromResult(new ValidationResult(false, "Newer reading already exists"));
        }

        return Task.FromResult(new ValidationResult(true));
    }
}