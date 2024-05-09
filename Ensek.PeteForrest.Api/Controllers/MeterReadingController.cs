using Ensek.PeteForrest.Services.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ensek.PeteForrest.Api.Controllers;

[ApiController]
[Route("")]
public class MeterReadingController(IMeterReadingService meterReadingService) : ControllerBase
{
    [HttpPost("meter-reading-uploads")]
    public async Task<MeterReadingUploadResult> UploadMeterReadingsAsync([FromBody] IEnumerable<MeterReadingLine> meterReadingLines)
    {
        var successes = 0;
        var failures = 0;
        foreach (var record in meterReadingLines)
        {
            if (DateTime.TryParse(record.MeterReadingDateTime, out var readingDateTime) &&
                await meterReadingService.TryAddReadingAsync(record.AccountId, record.MeterReadValue, readingDateTime))
                successes++;
            else
                failures++;
        }

        return new MeterReadingUploadResult(successes, failures);
    }

    public record MeterReadingLine
    {
        public int AccountId { get; set; }

        public string MeterReadingDateTime { get; set; }

        public string MeterReadValue { get; set; }
    }
}