using Ensek.PeteForrest.Services.Model;
using Ensek.PeteForrest.Services.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ensek.PeteForrest.Api.Controllers;

[ApiController]
[Route("")]
public class MeterReadingController(IMeterReadingService meterReadingService) : ControllerBase
{
    [HttpPost("meter-reading-uploads")]
    public async Task<MeterReadingUploadResult> UploadMeterReadingsAsync([FromBody] IAsyncEnumerable<MeterReadingLine> meterReadingLines)
    {
        var (successes, failures) = await meterReadingService.TryAddReadingsAsync(meterReadingLines);
        return new MeterReadingUploadResult(successes, failures);
    }
}