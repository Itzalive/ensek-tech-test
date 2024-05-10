using System.Globalization;
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
        public async Task TryAddReadingAsync_CanSuccessfullyAddMeterReading()
        {
            var accountRepositoryMock = new Mock<IAccountRepository>();
            var meterReadingRepositoryMock = new Mock<IMeterReadingRepository>();
            var service = new MeterReadingService(accountRepositoryMock.Object, meterReadingRepositoryMock.Object);
            accountRepositoryMock.Setup(a => a.GetAsync(1)).Returns(Task.FromResult(new Account { AccountId = 1 }));

            var result = await service.TryAddReadingAsync(new MeterReadingLine{AccountId = 1, MeterReadValue = "12345", MeterReadingDateTime = DateTime.UtcNow.ToString("dd/MM/yyyy hh:mm")});

            Assert.True(result);
            meterReadingRepositoryMock.Verify(m => m.Add(It.IsAny<MeterReading>()));
        }

        [Fact]
        public async Task TryAddReadingAsync_FailsWithMissingAccount()
        {
            var accountRepositoryMock = new Mock<IAccountRepository>();
            var meterReadingRepositoryMock = new Mock<IMeterReadingRepository>();
            var service = new MeterReadingService(accountRepositoryMock.Object, meterReadingRepositoryMock.Object);
            accountRepositoryMock.Setup(a => a.GetAsync(1)).Returns(Task.FromResult((Account?)null));

            var result = await service.TryAddReadingAsync(new MeterReadingLine{AccountId = 1, MeterReadValue = "12345", MeterReadingDateTime = DateTime.UtcNow.ToString("dd/MM/yyyy hh:mm")});

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
        public async Task TryAddReadingAsync_FailsWithInvalidFormatMeterReading(string value)
        {
            var accountRepositoryMock = new Mock<IAccountRepository>();
            var meterReadingRepositoryMock = new Mock<IMeterReadingRepository>();
            var service = new MeterReadingService(accountRepositoryMock.Object, meterReadingRepositoryMock.Object);
            accountRepositoryMock.Setup(a => a.GetAsync(1)).Returns(Task.FromResult(new Account { AccountId = 1 }));

            var result = await service.TryAddReadingAsync(new MeterReadingLine{AccountId = 1, MeterReadValue = value, MeterReadingDateTime = DateTime.UtcNow.ToString("dd/MM/yyyy hh:mm")});

            Assert.False(result);
            meterReadingRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task TryAddReadingAsync_FailsWhenAlreadyAdded()
        {
            var accountRepositoryMock = new Mock<IAccountRepository>();
            var meterReadingRepositoryMock = new Mock<IMeterReadingRepository>();
            var service = new MeterReadingService(accountRepositoryMock.Object, meterReadingRepositoryMock.Object);
            var existingReading = new MeterReading { DateTime = DateTime.Parse(DateTime.UtcNow.ToString("dd/MM/yyyy hh:mm"), new CultureInfo("en-gb")), Value = 41823 };
            accountRepositoryMock.Setup(a => a.GetAsync(1)).Returns(Task.FromResult(new Account { AccountId = 1, MeterReadings = [existingReading] }));

            var result =
                await service.TryAddReadingAsync(new MeterReadingLine { AccountId = 1, MeterReadValue = existingReading.Value.ToString(), MeterReadingDateTime = existingReading.DateTime.ToString("dd/MM/yyyy hh:mm") });


            Assert.False(result);
            meterReadingRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task TryAddReadingAsync_SucceedsWhenOnlyValueMatches()
        {
            var accountRepositoryMock = new Mock<IAccountRepository>();
            var meterReadingRepositoryMock = new Mock<IMeterReadingRepository>();
            var service = new MeterReadingService(accountRepositoryMock.Object, meterReadingRepositoryMock.Object);
            var existingReading = new MeterReading { DateTime = DateTime.UtcNow.Date, Value = 41823 };
            accountRepositoryMock.Setup(a => a.GetAsync(1)).Returns(Task.FromResult(new Account { AccountId = 1, MeterReadings = [existingReading] }));

            var result =
                await service.TryAddReadingAsync(new MeterReadingLine{AccountId = 1, MeterReadValue = existingReading.Value.ToString(), MeterReadingDateTime = existingReading.DateTime.AddDays(1).ToString("dd/MM/yyyy hh:mm")});
            
            Assert.True(result);
            meterReadingRepositoryMock.Verify(m => m.Add(It.IsAny<MeterReading>()));
        }

        [Fact]
        public async Task TryAddReadingAsync_FailsIfBeforeLastReading()
        {
            var accountRepositoryMock = new Mock<IAccountRepository>();
            var meterReadingRepositoryMock = new Mock<IMeterReadingRepository>();
            var service = new MeterReadingService(accountRepositoryMock.Object, meterReadingRepositoryMock.Object);
            var existingReading = new MeterReading { DateTime = DateTime.UtcNow.Date, Value = 41823 };
            accountRepositoryMock.Setup(a => a.GetAsync(1)).Returns(Task.FromResult(new Account { AccountId = 1, MeterReadings = [existingReading] }));

            var result =
                await service.TryAddReadingAsync(new MeterReadingLine{AccountId = 1, MeterReadValue = existingReading.Value.ToString(), MeterReadingDateTime = existingReading.DateTime.AddDays(-1).ToString("dd/MM/yyyy hh:mm")});

            Assert.False(result);
            meterReadingRepositoryMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("NOT A DATE")]
        [InlineData("22/04/2019 09:25:25:25")]
        public async Task TryAddReadingAsync_InvalidDatetime_ReturnsFailure(string dateTime)
        {
            var accountRepositoryMock = new Mock<IAccountRepository>();
            var meterReadingRepositoryMock = new Mock<IMeterReadingRepository>();
            var service = new MeterReadingService(accountRepositoryMock.Object, meterReadingRepositoryMock.Object);
            accountRepositoryMock.SetupGet(a => a.Query).Returns(new[]{new Account { AccountId = 1 }}.AsQueryable());
            meterReadingRepositoryMock.SetupGet(a => a.Query).Returns(new EnumerableQuery<MeterReading>([]));
            
            var result =
                    await service.TryAddReadingAsync(new MeterReadingLine{AccountId = 1, MeterReadValue = "12345", MeterReadingDateTime = dateTime});

            Assert.False(result);
            meterReadingRepositoryMock.VerifyNoOtherCalls();
        }


        [Fact(Skip = "Need to Mock IQueryable")]
        public async Task TryAddReadingsAsync_CanSuccessfullyAddMeterReading() {
            var accountRepositoryMock = new Mock<IAccountRepository>();
            var meterReadingRepositoryMock = new Mock<IMeterReadingRepository>();
            var service = new MeterReadingService(accountRepositoryMock.Object, meterReadingRepositoryMock.Object);
            accountRepositoryMock.Setup(a => a.GetAsync(1)).Returns(Task.FromResult(new Account { AccountId = 1 }));

            var (successes, failures) = await service.TryAddReadingsAsync([new MeterReadingLine { AccountId = 1, MeterReadValue = "12345", MeterReadingDateTime = DateTime.UtcNow.ToString("dd/MM/yyyy hh:mm") }]);

            Assert.Equal(1, successes);
            Assert.Equal(0, failures);
            meterReadingRepositoryMock.Verify(m => m.Add(It.IsAny<MeterReading>()));
        }

        [Fact(Skip = "Need to Mock IQueryable")]
        public async Task TryAddReadingsAsync_FailsWithMissingAccount() {
            var accountRepositoryMock = new Mock<IAccountRepository>();
            var meterReadingRepositoryMock = new Mock<IMeterReadingRepository>();
            var service = new MeterReadingService(accountRepositoryMock.Object, meterReadingRepositoryMock.Object);
            accountRepositoryMock.Setup(a => a.GetAsync(1)).Returns(Task.FromResult((Account?)null));

            var (successes, failures) = await service.TryAddReadingsAsync([new MeterReadingLine { AccountId = 1, MeterReadValue = "12345", MeterReadingDateTime = DateTime.UtcNow.ToString("dd/MM/yyyy hh:mm") }]);

            Assert.Equal(0, successes);
            Assert.Equal(1, failures);
            meterReadingRepositoryMock.VerifyNoOtherCalls();
        }

        [Theory(Skip = "Need to Mock IQueryable")]
        [InlineData("A")]
        [InlineData("ABCDE")]
        [InlineData("123")]
        [InlineData("123456")]
        [InlineData("12.34")]
        [InlineData("12.345")]
        [InlineData("1,234")]
        public async Task TryAddReadingsAsync_FailsWithInvalidFormatMeterReading(string value) {
            var accountRepositoryMock = new Mock<IAccountRepository>();
            var meterReadingRepositoryMock = new Mock<IMeterReadingRepository>();
            var service = new MeterReadingService(accountRepositoryMock.Object, meterReadingRepositoryMock.Object);
            accountRepositoryMock.Setup(a => a.GetAsync(1)).Returns(Task.FromResult(new Account { AccountId = 1 }));

            var (successes, failures) = await service.TryAddReadingsAsync([new MeterReadingLine { AccountId = 1, MeterReadValue = value, MeterReadingDateTime = DateTime.UtcNow.ToString("dd/MM/yyyy hh:mm") }]);

            Assert.Equal(0, successes);
            Assert.Equal(1, failures);
            meterReadingRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact(Skip = "Need to Mock IQueryable")]
        public async Task TryAddReadingsAsync_FailsWhenAlreadyAdded() {
            var accountRepositoryMock = new Mock<IAccountRepository>();
            var meterReadingRepositoryMock = new Mock<IMeterReadingRepository>();
            var service = new MeterReadingService(accountRepositoryMock.Object, meterReadingRepositoryMock.Object);
            var existingReading = new MeterReading { DateTime = DateTime.UtcNow, Value = 41823 };
            accountRepositoryMock.Setup(a => a.GetAsync(1)).Returns(Task.FromResult(new Account { AccountId = 1, MeterReadings = [existingReading] }));

            var (successes, failures) =
                await service.TryAddReadingsAsync([new MeterReadingLine { AccountId = 1, MeterReadValue = existingReading.Value.ToString(), MeterReadingDateTime = existingReading.DateTime.ToString("dd/MM/yyyy hh:mm") }]);

            Assert.Equal(0, successes);
            Assert.Equal(1, failures);
            meterReadingRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact(Skip = "Need to Mock IQueryable")]
        public async Task TryAddReadingsAsync_SucceedsWhenOnlyValueMatches() {
            var accountRepositoryMock = new Mock<IAccountRepository>();
            var meterReadingRepositoryMock = new Mock<IMeterReadingRepository>();
            var service = new MeterReadingService(accountRepositoryMock.Object, meterReadingRepositoryMock.Object);
            var existingReading = new MeterReading { DateTime = DateTime.UtcNow, Value = 41823 };
            accountRepositoryMock.Setup(a => a.GetAsync(1)).Returns(Task.FromResult(new Account { AccountId = 1, MeterReadings = [existingReading] }));

            var (successes, failures) =
                await service.TryAddReadingsAsync([new MeterReadingLine { AccountId = 1, MeterReadValue = existingReading.Value.ToString(), MeterReadingDateTime = existingReading.DateTime.AddDays(1).ToString("dd/MM/yyyy hh:mm") }]);

            Assert.Equal(1, successes);
            Assert.Equal(0, failures);
            meterReadingRepositoryMock.Verify(m => m.Add(It.IsAny<MeterReading>()));
        }

        [Fact(Skip = "Need to Mock IQueryable")]
        public async Task TryAddReadingsAsync_FailsIfBeforeLastReading() {
            var accountRepositoryMock = new Mock<IAccountRepository>();
            var meterReadingRepositoryMock = new Mock<IMeterReadingRepository>();
            var service = new MeterReadingService(accountRepositoryMock.Object, meterReadingRepositoryMock.Object);
            var existingReading = new MeterReading { DateTime = DateTime.UtcNow, Value = 41823 };
            accountRepositoryMock.Setup(a => a.GetAsync(1)).Returns(Task.FromResult(new Account { AccountId = 1, MeterReadings = [existingReading] }));

            var (successes, failures) =
                await service.TryAddReadingsAsync([new MeterReadingLine { AccountId = 1, MeterReadValue = existingReading.Value.ToString(), MeterReadingDateTime = existingReading.DateTime.AddDays(-1).ToString("dd/MM/yyyy hh:mm") }]);

            Assert.Equal(0, successes);
            Assert.Equal(1, failures);
            meterReadingRepositoryMock.VerifyNoOtherCalls();
        }

        [Theory(Skip = "Need to Mock IQueryable")]
        [InlineData("NOT A DATE")]
        [InlineData("22/04/2019 09:25:25:25")]
        public async Task TryAddReadingsAsync_InvalidDatetime_ReturnsFailure(string dateTime) {
            var accountRepositoryMock = new Mock<IAccountRepository>();
            var meterReadingRepositoryMock = new Mock<IMeterReadingRepository>();
            var service = new MeterReadingService(accountRepositoryMock.Object, meterReadingRepositoryMock.Object);
            accountRepositoryMock.Setup(a => a.GetAsync(1)).Returns(Task.FromResult(new Account { AccountId = 1 }));

            var (successes, failures) =
                    await service.TryAddReadingsAsync([new MeterReadingLine { AccountId = 1, MeterReadValue = "12345", MeterReadingDateTime = dateTime }]);

            Assert.Equal(0, successes);
            Assert.Equal(1, failures);
            meterReadingRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact(Skip = "Need to Mock IQueryable")]
        public async Task TryAddReadingsAsync_SameReadingTwice_AcceptsOnlyOne() {
            var accountRepositoryMock = new Mock<IAccountRepository>();
            var meterReadingRepositoryMock = new Mock<IMeterReadingRepository>();
            var service = new MeterReadingService(accountRepositoryMock.Object, meterReadingRepositoryMock.Object);
            accountRepositoryMock.Setup(a => a.GetAsync(1)).Returns(Task.FromResult(new Account { AccountId = 1, MeterReadings = [] }));

            var meterReadingLine = new MeterReadingLine { AccountId = 1, MeterReadValue = "12345", MeterReadingDateTime = DateTime.UtcNow.ToString("dd/MM/yyyy hh:mm") };
            var (successes, failures) =
                await service.TryAddReadingsAsync([meterReadingLine, meterReadingLine]);

            Assert.Equal(1, successes);
            Assert.Equal(1, failures);
        }
    }
}