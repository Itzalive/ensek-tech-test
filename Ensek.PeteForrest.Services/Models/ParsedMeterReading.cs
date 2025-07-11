using Ensek.PeteForrest.Domain;

namespace Ensek.PeteForrest.Services.Services;

public record ParsedMeterReading
{
    public required int RowId { get; init; }
    public required MeterReading MeterReading { get; init; }
}