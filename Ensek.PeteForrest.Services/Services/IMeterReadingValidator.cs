using Ensek.PeteForrest.Domain;
using Ensek.PeteForrest.Services.Models;

namespace Ensek.PeteForrest.Services.Services;

public interface IMeterReadingValidator
{
    ValueTask<ValidationResult> ValidateAsync(MeterReading reading, Account account, CancellationToken cancellationToken = default);
}