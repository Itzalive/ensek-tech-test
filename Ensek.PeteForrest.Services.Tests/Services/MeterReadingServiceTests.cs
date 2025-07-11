using System.Globalization;
using Ensek.PeteForrest.Domain;
using Ensek.PeteForrest.Domain.Repositories;
using Ensek.PeteForrest.Services.Model;
using Ensek.PeteForrest.Services.Models;
using Ensek.PeteForrest.Services.Services;
using Ensek.PeteForrest.Services.Services.Implementations;
using Microsoft.Extensions.Logging;
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
            var meterReadingParserMock = new Mock<IMeterReadingParser>();
            var service = new MeterReadingService(accountRepositoryMock.Object, meterReadingRepositoryMock.Object,
                meterReadingParserMock.Object, [], Mock.Of<ILogger<MeterReadingService>>());
            accountRepositoryMock.Setup(a => a.GetAsync(1)).ReturnsAsync(new Account
            {
                AccountId = 1
            });
            var parsedMeterReading = new ParsedMeterReading
            {
                RowId = 1,
                MeterReading = new MeterReading
                {
                    AccountId = 1,
                    DateTime = DateTime.UtcNow,
                    Value = 12345
                }
            };
            meterReadingParserMock.Setup(p => p.TryParse(It.IsAny<MeterReadingLine>(), out parsedMeterReading))
                .Returns(true);

            var result = await service.TryAddReadingAsync(new MeterReadingLine
            {
                AccountId = 1,
                MeterReadValue = "12345",
                MeterReadingDateTime = DateTime.UtcNow.ToString("dd/MM/yyyy hh:mm")
            });

            Assert.True(result);
            meterReadingRepositoryMock.Verify(m => m.Add(It.IsAny<MeterReading>()));
        }

        [Fact]
        public async Task TryAddReadingAsync_FailsWithMissingAccount()
        {
            var accountRepositoryMock = new Mock<IAccountRepository>();
            var meterReadingRepositoryMock = new Mock<IMeterReadingRepository>();
            var meterReadingParserMock = new Mock<IMeterReadingParser>();
            var service = new MeterReadingService(accountRepositoryMock.Object, meterReadingRepositoryMock.Object,
                meterReadingParserMock.Object, [], Mock.Of<ILogger<MeterReadingService>>());
            accountRepositoryMock.Setup(a => a.GetAsync(1)).Returns(Task.FromResult((Account?)null));

            var result = await service.TryAddReadingAsync(new MeterReadingLine
            {
                AccountId = 1,
                MeterReadValue = "12345",
                MeterReadingDateTime = DateTime.UtcNow.ToString("dd/MM/yyyy hh:mm")
            });

            Assert.False(result);
            meterReadingRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task TryAddReadingAsync_FailsWhenAlreadyAdded()
        {
            var accountRepositoryMock = new Mock<IAccountRepository>();
            var meterReadingRepositoryMock = new Mock<IMeterReadingRepository>();
            var meterReadingParserMock = new Mock<IMeterReadingParser>();
            var service = new MeterReadingService(accountRepositoryMock.Object, meterReadingRepositoryMock.Object,
                meterReadingParserMock.Object, [], Mock.Of<ILogger<MeterReadingService>>());
            var existingReading = new MeterReading
            {
                AccountId = 1,
                DateTime = DateTime.Parse(DateTime.UtcNow.ToString("dd/MM/yyyy hh:mm"), new CultureInfo("en-gb")),
                Value = 41823
            };
            accountRepositoryMock.Setup(a => a.GetAsync(1)).ReturnsAsync(new Account
            {
                AccountId = 1,
                MeterReadings = [existingReading]
            });

            var result =
                await service.TryAddReadingAsync(new MeterReadingLine
                {
                    AccountId = 1,
                    MeterReadValue = existingReading.Value.ToString(),
                    MeterReadingDateTime = existingReading.DateTime.ToString("dd/MM/yyyy hh:mm")
                });


            Assert.False(result);
            meterReadingRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task TryAddReadingAsync_FailsIfParsingFails()
        {
            var accountRepositoryMock = new Mock<IAccountRepository>();
            var meterReadingRepositoryMock = new Mock<IMeterReadingRepository>();
            var meterReadingParserMock = new Mock<IMeterReadingParser>();
            var service = new MeterReadingService(accountRepositoryMock.Object, meterReadingRepositoryMock.Object,
                meterReadingParserMock.Object, [], Mock.Of<ILogger<MeterReadingService>>());
            accountRepositoryMock.Setup(a => a.GetAsync(1)).ReturnsAsync(new Account
            {
                AccountId = 1
            });
            ParsedMeterReading parsedMeterReading = null!;
            meterReadingParserMock.Setup(p => p.TryParse(It.IsAny<MeterReadingLine>(), out parsedMeterReading))
                .Returns(false);


            var result =
                await service.TryAddReadingAsync(new MeterReadingLine
                {
                    AccountId = 1,
                    MeterReadValue = "12345",
                    MeterReadingDateTime = DateTime.UtcNow.ToString("dd/MM/yyyy hh:mm")
                });

            Assert.False(result);
            meterReadingRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task TryAddReadingAsync_FailsIfValidationFails()
        {
            var accountRepositoryMock = new Mock<IAccountRepository>();
            var meterReadingRepositoryMock = new Mock<IMeterReadingRepository>();
            var meterReadingParserMock = new Mock<IMeterReadingParser>();
            var meterReadingValidatorMock = new Mock<IMeterReadingValidator>();
            var service = new MeterReadingService(accountRepositoryMock.Object, meterReadingRepositoryMock.Object,
                meterReadingParserMock.Object, [meterReadingValidatorMock.Object], Mock.Of<ILogger<MeterReadingService>>());
            accountRepositoryMock.Setup(a => a.GetAsync(1)).ReturnsAsync(new Account
            {
                AccountId = 1
            });
            var parsedMeterReading = new ParsedMeterReading
            {
                RowId = 1,
                MeterReading = new MeterReading
                {
                    AccountId = 1,
                    DateTime = DateTime.UtcNow,
                    Value = 12345
                }
            };
            meterReadingParserMock.Setup(p => p.TryParse(It.IsAny<MeterReadingLine>(), out parsedMeterReading))
                .Returns(true);
            meterReadingValidatorMock.Setup(v => v.ValidateAsync(parsedMeterReading.MeterReading, It.IsAny<Account>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(false, "Validation failed"));

            var result =
                await service.TryAddReadingAsync(new MeterReadingLine
                {
                    AccountId = 1,
                    MeterReadValue = "12345",
                    MeterReadingDateTime = DateTime.UtcNow.ToString("dd/MM/yyyy hh:mm")
                });

            Assert.False(result);
            meterReadingRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task TryAddReadingsAsync_CanSuccessfullyAddMeterReading()
        {
            var accountRepositoryMock = new Mock<IAccountRepository>();
            var meterReadingRepositoryMock = new Mock<IMeterReadingRepository>();
            var meterReadingParserMock = new Mock<IMeterReadingParser>();
            var service = new MeterReadingService(accountRepositoryMock.Object, meterReadingRepositoryMock.Object,
                meterReadingParserMock.Object, [], Mock.Of<ILogger<MeterReadingService>>());
            accountRepositoryMock.Setup(a => a.GetAsync(It.IsAny<IEnumerable<int>>())).Returns(
                Task.FromResult<Account[]>([
                    new Account
                    {
                        AccountId = 1
                    }
                ]));
            var parsedMeterReading = new ParsedMeterReading
            {
                RowId = 1,
                MeterReading = new MeterReading
                {
                    AccountId = 1,
                    DateTime = DateTime.UtcNow,
                    Value = 12345
                }
            };
            meterReadingParserMock.Setup(p => p.TryParse(It.IsAny<MeterReadingLine>(), out parsedMeterReading))
                .Returns(true);

            var (successes, failures) = await service.TryAddReadingsAsync(new[]
            {
                new MeterReadingLine
                {
                    AccountId = 1,
                    MeterReadValue = "12345",
                    MeterReadingDateTime = DateTime.UtcNow.ToString("dd/MM/yyyy hh:mm")
                }
            }.ToAsyncEnumerable());

            Assert.Equal(1, successes);
            Assert.Equal(0, failures);
            meterReadingRepositoryMock.Verify(m => m.Add(It.IsAny<MeterReading>()));
        }

        [Fact]
        public async Task TryAddReadingsAsync_FailsWithMissingAccount()
        {
            var accountRepositoryMock = new Mock<IAccountRepository>();
            var meterReadingRepositoryMock = new Mock<IMeterReadingRepository>();
            var meterReadingParserMock = new Mock<IMeterReadingParser>();
            var service = new MeterReadingService(accountRepositoryMock.Object, meterReadingRepositoryMock.Object,
                meterReadingParserMock.Object, [], Mock.Of<ILogger<MeterReadingService>>());
            accountRepositoryMock.Setup(a => a.GetAsync(It.IsAny<IEnumerable<int>>()))
                .Returns(Task.FromResult(Array.Empty<Account>()));

            var (successes, failures) = await service.TryAddReadingsAsync(new[]
            {
                new MeterReadingLine
                {
                    AccountId = 1,
                    MeterReadValue = "12345",
                    MeterReadingDateTime = DateTime.UtcNow.ToString("dd/MM/yyyy hh:mm")
                }
            }.ToAsyncEnumerable());

            Assert.Equal(0, successes);
            Assert.Equal(1, failures);
            meterReadingRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task TryAddReadingsAsync_FailsWhenAlreadyAdded()
        {
            var accountRepositoryMock = new Mock<IAccountRepository>();
            var meterReadingRepositoryMock = new Mock<IMeterReadingRepository>();
            var meterReadingParserMock = new Mock<IMeterReadingParser>();
            var service = new MeterReadingService(accountRepositoryMock.Object, meterReadingRepositoryMock.Object,
                meterReadingParserMock.Object, [], Mock.Of<ILogger<MeterReadingService>>());
            var existingReading = new MeterReading
            {
                AccountId = 1,
                DateTime = DateTime.UtcNow,
                Value = 41823
            };
            accountRepositoryMock.Setup(a => a.GetAsync(It.IsAny<IEnumerable<int>>())).Returns(Task.FromResult(new[]
            {
                new Account
                {
                    AccountId = 1,
                    CurrentMeterReading = existingReading,
                    MeterReadings = [existingReading]
                }
            }));

            var (successes, failures) =
                await service.TryAddReadingsAsync(new[]
                {
                    new MeterReadingLine
                    {
                        AccountId = 1,
                        MeterReadValue = existingReading.Value.ToString(),
                        MeterReadingDateTime = existingReading.DateTime.ToString("dd/MM/yyyy hh:mm")
                    }
                }.ToAsyncEnumerable());

            Assert.Equal(0, successes);
            Assert.Equal(1, failures);
            meterReadingRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task TryAddReadingsAsync_FailsIfParsingFails()
        {
            var accountRepositoryMock = new Mock<IAccountRepository>();
            var meterReadingRepositoryMock = new Mock<IMeterReadingRepository>();
            var meterReadingParserMock = new Mock<IMeterReadingParser>();
            var service = new MeterReadingService(accountRepositoryMock.Object, meterReadingRepositoryMock.Object,
                meterReadingParserMock.Object, [], Mock.Of<ILogger<MeterReadingService>>());
            accountRepositoryMock.Setup(a => a.GetAsync(1)).ReturnsAsync(new Account
            {
                AccountId = 1
            });
            ParsedMeterReading parsedMeterReading = null!;
            meterReadingParserMock.Setup(p => p.TryParse(It.IsAny<MeterReadingLine>(), out parsedMeterReading))
                .Returns(false);

            var (successes, failures) =
                await service.TryAddReadingsAsync(new[]
                {
                    new MeterReadingLine
                    {
                        AccountId = 1,
                        MeterReadValue = "12345",
                        MeterReadingDateTime = DateTime.UtcNow.ToString("dd/MM/yyyy hh:mm")
                    }
                }.ToAsyncEnumerable());

            Assert.Equal(0, successes);
            Assert.Equal(1, failures);
            meterReadingRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task TryAddReadingsAsync_FailsIfValidationFails()
        {
            var accountRepositoryMock = new Mock<IAccountRepository>();
            var meterReadingRepositoryMock = new Mock<IMeterReadingRepository>();
            var meterReadingParserMock = new Mock<IMeterReadingParser>();
            var meterReadingValidatorMock = new Mock<IMeterReadingValidator>();
            var service = new MeterReadingService(accountRepositoryMock.Object, meterReadingRepositoryMock.Object,
                meterReadingParserMock.Object, [meterReadingValidatorMock.Object], Mock.Of<ILogger<MeterReadingService>>());
            accountRepositoryMock.Setup(a => a.GetAsync(1)).ReturnsAsync(new Account
            {
                AccountId = 1
            });
            var parsedMeterReading = new ParsedMeterReading
            {
                RowId = 1,
                MeterReading = new MeterReading
                {
                    AccountId = 1,
                    DateTime = DateTime.UtcNow,
                    Value = 12345
                }
            };
            meterReadingParserMock.Setup(p => p.TryParse(It.IsAny<MeterReadingLine>(), out parsedMeterReading))
                .Returns(true);
            meterReadingValidatorMock.Setup(v => v.ValidateAsync(parsedMeterReading.MeterReading, It.IsAny<Account>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(false, "Validation failed"));

            var (successes, failures) =
                await service.TryAddReadingsAsync(new[]
                {
                    new MeterReadingLine
                    {
                        AccountId = 1,
                        MeterReadValue = "12345",
                        MeterReadingDateTime = DateTime.UtcNow.ToString("dd/MM/yyyy hh:mm")
                    }
                }.ToAsyncEnumerable());

            Assert.Equal(0, successes);
            Assert.Equal(1, failures);
            meterReadingRepositoryMock.VerifyNoOtherCalls();
        }
    }
}