using Ensek.PeteForrest.Services.Model;

namespace Ensek.PeteForrest.Services.Services;

public interface IMeterReadingParser
{
    bool TryParse(MeterReadingLine reading, out ParsedMeterReading parsedReading);
}