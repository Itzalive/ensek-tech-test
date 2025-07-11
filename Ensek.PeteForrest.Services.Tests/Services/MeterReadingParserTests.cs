using Ensek.PeteForrest.Services.Model;
using Ensek.PeteForrest.Services.Services.Implementations;

namespace Ensek.PeteForrest.Services.Tests.Services;

public class MeterReadingParserTests
{
    [Theory]
    [InlineData("A")]
    [InlineData("ABCDE")]
    [InlineData("123")]
    [InlineData("123456")]
    [InlineData("12.34")]
    [InlineData("12.345")]
    [InlineData("1,234")]
    public void TryParse_FailsWithInvalidFormatMeterReading(string value)
    {
        var service = new MeterReadingParser();

        var result = service.TryParse(new MeterReadingLine
        {
            AccountId = 1,
            MeterReadValue = value,
            MeterReadingDateTime = DateTime.UtcNow.ToString("dd/MM/yyyy hh:mm")
        }, out _);

        Assert.False(result);
    }

    [Theory]
    [InlineData("NOT A DATE")]
    [InlineData("22/04/2019 09:25:25:25")]
    public void TryParse_InvalidDatetime_ReturnsFailure(string dateTime)
    {
        var service = new MeterReadingParser();

        var result = service.TryParse(new MeterReadingLine
        {
            AccountId = 1,
            MeterReadValue = "12345",
            MeterReadingDateTime = dateTime
        }, out _);

        Assert.False(result);
    }
}