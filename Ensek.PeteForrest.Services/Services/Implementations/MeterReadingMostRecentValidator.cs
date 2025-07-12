using Ensek.PeteForrest.Domain;
using Ensek.PeteForrest.Services.Models;

namespace Ensek.PeteForrest.Services.Services.Implementations;

internal class MeterReadingMostRecentValidator : IMeterReadingValidator
{
    public ValueTask<ValidationResult> ValidateAsync(MeterReading reading, Account account, CancellationToken cancellationToken = default)
    {
        // Confirm the reading is the newest reading
        if (account.CurrentMeterReading != null &&
            account.CurrentMeterReading.DateTime >= reading.DateTime)
        {
            return ValueTask.FromResult(new ValidationResult(false, "Newer reading already exists"));
        }

        return ValueTask.FromResult(new ValidationResult(true));
    }
}