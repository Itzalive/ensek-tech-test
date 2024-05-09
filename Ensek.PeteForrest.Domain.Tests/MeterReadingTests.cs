namespace Ensek.PeteForrest.Domain.Tests {
    public class MeterReadingTests {
        [Theory]
        [InlineData("12345", 12345)]
        [InlineData("91823", 91823)]
        [InlineData("00003", 3)]
        [InlineData("00000", 0)]
        public void TryParseValue_ParsesSuccessfulValues(string value, int expectedResult)
        {
            var tryParseValue = MeterReading.TryParseValue(value, out var result);
            Assert.True(tryParseValue);
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("-2345")]
        [InlineData("3")]
        [InlineData("ABCDE")]
        [InlineData("1,123")]
        [InlineData("1.123")]
        [InlineData("12.435")]
        [InlineData("1223.")]
        [InlineData("122.0")]
        public void TryParseValue_FailsToParseInvalidValues(string value)
        {
            var tryParseValue = MeterReading.TryParseValue(value, out var result);
            Assert.False(tryParseValue);
        }
    }
}