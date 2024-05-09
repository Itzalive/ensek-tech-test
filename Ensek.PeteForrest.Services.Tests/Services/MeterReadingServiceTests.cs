using Ensek.PeteForrest.Domain;
using Ensek.PeteForrest.Services.Data;
using Ensek.PeteForrest.Services.Services;
using Moq;

namespace Ensek.PeteForrest.Services.Tests.Services
{
    public class MeterReadingServiceTests
    {
        [Fact]
        public async Task CanSuccessfullyAddMeterReading()
        {
            var accountRepositoryMock = new Mock<IAccountRepository>();
            var meterReadingRepositoryMock = new Mock<IMeterReadingRepository>();
            var service = new MeterReadingService(accountRepositoryMock.Object, meterReadingRepositoryMock.Object);
            accountRepositoryMock.Setup(a => a.GetAsync(1)).Returns(Task.FromResult(new Account { AccountId = 1 }));

            var result = await service.TryAddReadingAsync(1, "12345", DateTime.UtcNow);

            Assert.True(result);
            meterReadingRepositoryMock.Verify(m => m.Add(It.IsAny<MeterReading>()));
        }

        [Fact]
        public async Task AddingMeterReadingFailsWithMissingAccount()
        {
            var accountRepositoryMock = new Mock<IAccountRepository>();
            var meterReadingRepositoryMock = new Mock<IMeterReadingRepository>();
            var service = new MeterReadingService(accountRepositoryMock.Object, meterReadingRepositoryMock.Object);
            accountRepositoryMock.Setup(a => a.GetAsync(1)).Returns(Task.FromResult((Account?)null));

            var result = await service.TryAddReadingAsync(1, "12345", DateTime.UtcNow);

            Assert.False(result);
            meterReadingRepositoryMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("A")]
        [InlineData("ABCDE")]
        [InlineData("123")]
        [InlineData("123456")]
        [InlineData("12.34")]
        [InlineData("12.345")]
        [InlineData("1,234")]
        public async Task AddingMeterReadingFailsWithInvalidFormatMeterReading(string value)
        {
            var accountRepositoryMock = new Mock<IAccountRepository>();
            var meterReadingRepositoryMock = new Mock<IMeterReadingRepository>();
            var service = new MeterReadingService(accountRepositoryMock.Object, meterReadingRepositoryMock.Object);
            accountRepositoryMock.Setup(a => a.GetAsync(1)).Returns(Task.FromResult(new Account { AccountId = 1 }));

            var result = await service.TryAddReadingAsync(1, value, DateTime.UtcNow);

            Assert.False(result);
            meterReadingRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task AddingMeterReadingFailsWhenAlreadyAdded()
        {
            var accountRepositoryMock = new Mock<IAccountRepository>();
            var meterReadingRepositoryMock = new Mock<IMeterReadingRepository>();
            var service = new MeterReadingService(accountRepositoryMock.Object, meterReadingRepositoryMock.Object);
            var existingReading = new MeterReading { DateTime = DateTime.UtcNow, Value = 41823 };
            accountRepositoryMock.Setup(a => a.GetAsync(1)).Returns(Task.FromResult(new Account { AccountId = 1, MeterReadings = [existingReading] }));

            var result =
                await service.TryAddReadingAsync(1, existingReading.Value.ToString(), existingReading.DateTime);

            Assert.False(result);
            meterReadingRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task AddingMeterReadingSucceedsWhenOnlyValueMatches()
        {
            var accountRepositoryMock = new Mock<IAccountRepository>();
            var meterReadingRepositoryMock = new Mock<IMeterReadingRepository>();
            var service = new MeterReadingService(accountRepositoryMock.Object, meterReadingRepositoryMock.Object);
            var existingReading = new MeterReading { DateTime = DateTime.UtcNow, Value = 41823 };
            accountRepositoryMock.Setup(a => a.GetAsync(1)).Returns(Task.FromResult(new Account { AccountId = 1, MeterReadings = [existingReading] }));

            var result =
                await service.TryAddReadingAsync(1, existingReading.Value.ToString(),
                    existingReading.DateTime.AddDays(1));
            
            Assert.True(result);
            meterReadingRepositoryMock.Verify(m => m.Add(It.IsAny<MeterReading>()));
        }

        [Fact]
        public async Task AddingMeterReadingFailsIfBeforeLastReading()
        {
            var accountRepositoryMock = new Mock<IAccountRepository>();
            var meterReadingRepositoryMock = new Mock<IMeterReadingRepository>();
            var service = new MeterReadingService(accountRepositoryMock.Object, meterReadingRepositoryMock.Object);
            var existingReading = new MeterReading { DateTime = DateTime.UtcNow, Value = 41823 };
            accountRepositoryMock.Setup(a => a.GetAsync(1)).Returns(Task.FromResult(new Account { AccountId = 1, MeterReadings = [existingReading] }));

            var result =
                await service.TryAddReadingAsync(1, existingReading.Value.ToString(),
                    existingReading.DateTime.AddDays(-1));

            Assert.False(result);
            meterReadingRepositoryMock.VerifyNoOtherCalls();
        }
    }
}