namespace Ensek.PeteForrest.Services.Model;

public struct MeterReadingLine
{
    public int RowId { get; init; }

    public int? AccountId { get; init; }

    public string? MeterReadingDateTime { get; init; }

    public string? MeterReadValue { get; init; }

    public ParseErrors ParseErrors { get; init; }
}

[Flags]
public enum ParseErrors
{
    None = 0,
    IncompleteData = 1,
    InvalidAccountId = 2,
    InvalidMeterReadingDateTime = 4,
    InvalidMeterReadValue = 8,
}