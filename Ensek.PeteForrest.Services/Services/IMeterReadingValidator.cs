using Ensek.PeteForrest.Domain;

namespace Ensek.PeteForrest.Services.Services;

public interface IMeterReadingValidator
{
    Task<ValidationResult> ValidateAsync(MeterReading reading, Account account, CancellationToken cancellationToken = default);
}