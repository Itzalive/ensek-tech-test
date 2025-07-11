using Ensek.PeteForrest.Services.Model;
using Ensek.PeteForrest.Services.Models;

namespace Ensek.PeteForrest.Services.Services;

public interface IMeterReadingParser
{
    bool TryParse(MeterReadingLine reading, out ParsedMeterReading parsedReading);
}