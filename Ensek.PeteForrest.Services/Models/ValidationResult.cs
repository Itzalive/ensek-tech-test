namespace Ensek.PeteForrest.Services.Models;

public record ValidationResult(bool IsValid, string? Error = null);