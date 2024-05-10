using System.Globalization;
using Ensek.PeteForrest.Services.Model;
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
            if (await meterReadingService.TryAddReadingAsync(record))
                successes++;
            else
                failures++;
        }

        return new MeterReadingUploadResult(successes, failures);
    }
}