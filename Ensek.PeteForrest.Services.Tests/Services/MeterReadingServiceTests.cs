using Ensek.PeteForrest.Domain;
using Ensek.PeteForrest.Services.Data;
using Ensek.PeteForrest.Services.Model;
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

            var result = await service.TryAddReadingAsync(new MeterReadingLine{AccountId = 1, MeterReadValue = "12345", MeterReadingDateTime = DateTime.UtcNow.ToString()});

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

            var result = await service.TryAddReadingAsync(new MeterReadingLine{AccountId = 1, MeterReadValue = "12345", MeterReadingDateTime = DateTime.UtcNow.ToString()});

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

            var result = await service.TryAddReadingAsync(new MeterReadingLine{AccountId = 1, MeterReadValue = value, MeterReadingDateTime = DateTime.UtcNow.ToString()});

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
                await service.TryAddReadingAsync(new MeterReadingLine{AccountId = 1, MeterReadValue = existingReading.Value.ToString(), MeterReadingDateTime = existingReading.DateTime.ToString()});

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
                await service.TryAddReadingAsync(new MeterReadingLine{AccountId = 1, MeterReadValue = existingReading.Value.ToString(), MeterReadingDateTime = existingReading.DateTime.AddDays(1).ToString()});
            
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
                await service.TryAddReadingAsync(new MeterReadingLine{AccountId = 1, MeterReadValue = existingReading.Value.ToString(), MeterReadingDateTime = existingReading.DateTime.AddDays(-1).ToString()});

            Assert.False(result);
            meterReadingRepositoryMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("NOT A DATE")]
        [InlineData("22/04/2019 09:25:25:25")]
        public async Task InvalidDatetime_ReturnsFailure(string dateTime)
        {
            var accountRepositoryMock = new Mock<IAccountRepository>();
            var meterReadingRepositoryMock = new Mock<IMeterReadingRepository>();
            var service = new MeterReadingService(accountRepositoryMock.Object, meterReadingRepositoryMock.Object);
            accountRepositoryMock.Setup(a => a.GetAsync(1)).Returns(Task.FromResult(new Account { AccountId = 1 }));
            
            var result =
                    await service.TryAddReadingAsync(new MeterReadingLine{AccountId = 1, MeterReadValue = "12345", MeterReadingDateTime = dateTime});

            Assert.False(result);
            meterReadingRepositoryMock.VerifyNoOtherCalls();
        }
    }
}