namespace Ensek.PeteForrest.Services.Model;

[Flags]
public enum ParseErrors
{
    None = 0,
    IncompleteData = 1,
    InvalidAccountId = 2,
    InvalidMeterReadingDateTime = 4,
    InvalidMeterReadValue = 8,
}