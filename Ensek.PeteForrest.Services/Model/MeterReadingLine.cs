namespace Ensek.PeteForrest.Services.Model;

public record MeterReadingLine
{
    public int AccountId { get; set; }

    public string MeterReadingDateTime { get; set; }

    public string MeterReadValue { get; set; }
}