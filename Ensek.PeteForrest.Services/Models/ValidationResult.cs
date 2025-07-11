namespace Ensek.PeteForrest.Services.Services;

public record ValidationResult(bool IsValid, string? Error = null);