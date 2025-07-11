using Ensek.PeteForrest.Domain;
using Ensek.PeteForrest.Services.Services.Implementations;

namespace Ensek.PeteForrest.Services.Tests.Services
{
    public class MeterReadingMostRecentValidatorTests
    {
        private readonly MeterReadingMostRecentValidator _validator = new();

        // Tests written by Claude AI

        [Fact]
        public async Task ValidateAsync_NoCurrentReading_ReturnsValid()
        {
            var reading = new MeterReading { AccountId = 1, DateTime = DateTime.UtcNow, Value = 100 };
            var account = new Account { AccountId = 1, CurrentMeterReading = null };

            var result = await _validator.ValidateAsync(reading, account);

            Assert.True(result.IsValid);
            Assert.Null(result.Error);
        }

        [Fact]
        public async Task ValidateAsync_ReadingIsNewer_ReturnsValid()
        {
            var now = DateTime.UtcNow;
            var reading = new MeterReading { AccountId = 1, DateTime = now.AddMinutes(1), Value = 200 };
            var account = new Account
            {
                AccountId = 1,
                CurrentMeterReading = new MeterReading { AccountId = 1, DateTime = now, Value = 100 }
            };

            var result = await _validator.ValidateAsync(reading, account);

            Assert.True(result.IsValid);
            Assert.Null(result.Error);
        }

        [Fact]
        public async Task ValidateAsync_ReadingIsOlder_ReturnsInvalid()
        {
            var now = DateTime.UtcNow;
            var reading = new MeterReading { AccountId = 1, DateTime = now.AddMinutes(-1), Value = 50 };
            var account = new Account
            {
                AccountId = 1,
                CurrentMeterReading = new MeterReading { AccountId = 1, DateTime = now, Value = 100 }
            };

            var result = await _validator.ValidateAsync(reading, account);

            Assert.False(result.IsValid);
            Assert.Equal("Newer reading already exists", result.Error);
        }

        [Fact]
        public async Task ValidateAsync_ReadingIsSameTime_ReturnsInvalid()
        {
            var now = DateTime.UtcNow;
            var reading = new MeterReading { AccountId = 1, DateTime = now, Value = 100 };
            var account = new Account
            {
                AccountId = 1,
                CurrentMeterReading = new MeterReading { AccountId = 1, DateTime = now, Value = 100 }
            };

            var result = await _validator.ValidateAsync(reading, account);

            Assert.False(result.IsValid);
            Assert.Equal("Newer reading already exists", result.Error);
        }
    }
}
