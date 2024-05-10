using Ensek.PeteForrest.Api.Controllers;
using Ensek.PeteForrest.Services.Model;
using Ensek.PeteForrest.Services.Services;
using Moq;

namespace Ensek.PeteForrest.Api.Tests;

public class MeterReadingControllerTests
{
    [Fact]
    public async Task ValidRecord_ReturnsSuccess()
    {
        var meterReadingService = new Mock<IMeterReadingService>();
        var controller = new MeterReadingController(meterReadingService.Object);
        meterReadingService.Setup(m => m.TryAddReadingsAsync(It.IsAny<IEnumerable<MeterReadingLine>>()))
            .Returns(Task.FromResult((1, 0)));

        var result = await controller.UploadMeterReadingsAsync([
            new MeterReadingLine
                { AccountId = 1, MeterReadValue = "01002", MeterReadingDateTime = "22/04/2019 09:25" }
        ]);

        Assert.NotNull(result);
        Assert.Equal(1, result.NumberOfSuccessfulReadings);
        Assert.Equal(0, result.NumberOfFailedReadings);
    }

    [Fact]
    public async Task InvalidRecord_ReturnsFailure()
    {
        var meterReadingService = new Mock<IMeterReadingService>();
        var controller = new MeterReadingController(meterReadingService.Object);
        meterReadingService.Setup(m => m.TryAddReadingsAsync(It.IsAny<IEnumerable<MeterReadingLine>>()))
            .Returns(Task.FromResult((0, 1)));

        var result = await controller.UploadMeterReadingsAsync([
            new MeterReadingLine
                { AccountId = 1, MeterReadValue = "01002", MeterReadingDateTime = "22/04/2019 09:25" }
        ]);

        Assert.NotNull(result);
        Assert.Equal(0, result.NumberOfSuccessfulReadings);
        Assert.Equal(1, result.NumberOfFailedReadings);
    }
}