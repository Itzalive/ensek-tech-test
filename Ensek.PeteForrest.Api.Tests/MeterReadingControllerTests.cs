using Ensek.PeteForrest.Api.Controllers;
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
        meterReadingService.Setup(m => m.TryAddReadingAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .Returns(Task.FromResult(true));

        var result = await controller.UploadMeterReadingsAsync([
            new MeterReadingController.MeterReadingLine
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
        meterReadingService.Setup(m => m.TryAddReadingAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .Returns(Task.FromResult(false));

        var result = await controller.UploadMeterReadingsAsync([
            new MeterReadingController.MeterReadingLine
                { AccountId = 1, MeterReadValue = "01002", MeterReadingDateTime = "22/04/2019 09:25" }
        ]);

        Assert.NotNull(result);
        Assert.Equal(0, result.NumberOfSuccessfulReadings);
        Assert.Equal(1, result.NumberOfFailedReadings);
    }

    [Theory]
    [InlineData("NOT A DATE")]
    [InlineData("22/04/2019 09:25:25:25")]
    public async Task InvalidDatetime_ReturnsFailure(string dateTime)
    {
        var meterReadingService = new Mock<IMeterReadingService>();
        var controller = new MeterReadingController(meterReadingService.Object);
        meterReadingService.Setup(m => m.TryAddReadingAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .Returns(Task.FromResult(true));

        var result = await controller.UploadMeterReadingsAsync([
            new MeterReadingController.MeterReadingLine
                { AccountId = 1, MeterReadValue = "01002", MeterReadingDateTime = dateTime }
        ]);

        Assert.NotNull(result);
        Assert.Equal(0, result.NumberOfSuccessfulReadings);
        Assert.Equal(1, result.NumberOfFailedReadings);
        meterReadingService.VerifyNoOtherCalls();
    }
}