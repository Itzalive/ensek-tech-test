using System.Globalization;
using Ensek.PeteForrest.Domain;
using Ensek.PeteForrest.Services.Model;
using Ensek.PeteForrest.Services.Models;

namespace Ensek.PeteForrest.Services.Services.Implementations;

internal class MeterReadingParser : IMeterReadingParser
{
    private readonly CultureInfo _gbCulture = CultureInfo.CreateSpecificCulture("en-gb");

    public bool TryParse(MeterReadingLine reading, out ParsedMeterReading parsedReading)
    {
        if (!reading.AccountId.HasValue)
        {
            parsedReading = null!;
            return false;
        }

        // Parse DateTime
        if (string.IsNullOrEmpty(reading.MeterReadingDateTime) ||
            !DateTime.TryParse(reading.MeterReadingDateTime, _gbCulture,
                out var dateTime) && !DateTime.TryParse(reading.MeterReadingDateTime, CultureInfo.InvariantCulture,
                out dateTime))
        {
            parsedReading = null!;
            return false;
        }

        // Parse the reading value
        if (string.IsNullOrEmpty(reading.MeterReadValue) ||
            !MeterReading.TryParseValue(reading.MeterReadValue, out var intValueResult))
        {
            parsedReading = null!;
            return false;
        }

        parsedReading = new ParsedMeterReading
        {
            RowId = reading.RowId,
            MeterReading = new MeterReading
            {
                AccountId = reading.AccountId.Value,
                DateTime = dateTime,
                Value = intValueResult
            }
        };
        return true;
    }
}