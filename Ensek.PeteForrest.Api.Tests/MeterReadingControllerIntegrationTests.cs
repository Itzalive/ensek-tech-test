using System.Net;
using System.Net.Http.Headers;
using Ensek.PeteForrest.Domain;
using Ensek.PeteForrest.Services.Model;
using Newtonsoft.Json;

namespace Ensek.PeteForrest.Api.Tests;

public class MeterReadingControllerIntegrationTests(ApiHostFixture apiHostFixture) : IClassFixture<ApiHostFixture>
{
    [Fact]
    public async Task CanReadExampleCsv()
    {
        var stringContent = new StringContent(await File.ReadAllTextAsync("Data/Meter_Reading 2.csv"), MediaTypeHeaderValue.Parse("text/csv"));
        var response = await apiHostFixture.Client.PostAsync("meter-reading-uploads", stringContent);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<MeterReadingUploadResult>(responseBody);
        Assert.NotNull(result);
        Assert.Equal(25, result.NumberOfSuccessfulReadings);
        Assert.Equal(10, result.NumberOfFailedReadings);
    }

    [Fact]
    public async Task ReadsValidCsv_ReturnsSuccess()
    {
        var entity = new Account();
        apiHostFixture.Context.Accounts.Add(entity);
        await apiHostFixture.Context.SaveChangesAsync();
        var stringContent = new StringContent($"AccountId,MeterReadingDateTime,MeterReadValue\r\n{entity.AccountId},{"22/04/2019 09:25"},{"01002"}", MediaTypeHeaderValue.Parse("text/csv"));
        var response = await apiHostFixture.Client.PostAsync("meter-reading-uploads", stringContent);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<MeterReadingUploadResult>(responseBody);
        Assert.NotNull(result);
        Assert.Equal(1, result.NumberOfSuccessfulReadings);
        Assert.Equal(0, result.NumberOfFailedReadings);
    }

    [Fact]
    public async Task ReadsValidJson_ReturnsSuccess()
    {
        var entity = new Account();
        apiHostFixture.Context.Accounts.Add(entity);
        await apiHostFixture.Context.SaveChangesAsync();
        var stringContent = new StringContent($"[{{\"AccountId\":{entity.AccountId},\"MeterReadingDateTime\":\"22/04/2019 09:25\",\"MeterReadValue\":\"01002\"}}]", MediaTypeHeaderValue.Parse("text/json"));
        var response = await apiHostFixture.Client.PostAsync("meter-reading-uploads", stringContent);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<MeterReadingUploadResult>(responseBody);
        Assert.NotNull(result);
        Assert.Equal(1, result.NumberOfSuccessfulReadings);
        Assert.Equal(0, result.NumberOfFailedReadings);
    }

    [Fact]
    public async Task MismatchingAccountId_ReturnsFailure()
    {
        var entity = new Account();
        apiHostFixture.Context.Accounts.Add(entity);
        await apiHostFixture.Context.SaveChangesAsync();
        var stringContent = new StringContent($"AccountId,MeterReadingDateTime,MeterReadValue\r\n{9999999},{"22/04/2019 09:25"},{"01002"}", MediaTypeHeaderValue.Parse("text/csv"));
        var response = await apiHostFixture.Client.PostAsync("meter-reading-uploads", stringContent);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<MeterReadingUploadResult>(responseBody);
        Assert.NotNull(result);
        Assert.Equal(0, result.NumberOfSuccessfulReadings);
        Assert.Equal(1, result.NumberOfFailedReadings);
    }

    [Theory]
    [InlineData("NOT A DATE")]
    [InlineData("22/04/2019 09:25:26:26:26")]
    public async Task InvalidDateTime_ReturnsFailure(string dateTime)
    {
        var entity = new Account();
        apiHostFixture.Context.Accounts.Add(entity);
        await apiHostFixture.Context.SaveChangesAsync();
        var stringContent = new StringContent($"AccountId,MeterReadingDateTime,MeterReadValue\r\n{entity.AccountId},{dateTime},{"01002"}", MediaTypeHeaderValue.Parse("text/csv"));
        var response = await apiHostFixture.Client.PostAsync("meter-reading-uploads", stringContent);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<MeterReadingUploadResult>(responseBody);
        Assert.NotNull(result);
        Assert.Equal(0, result.NumberOfSuccessfulReadings);
        Assert.Equal(1, result.NumberOfFailedReadings);
    }

    [Fact]
    public async Task SameReadingTwice_AcceptsOnlyOne()
    {
        var entity = new Account();
        apiHostFixture.Context.Accounts.Add(entity);
        await apiHostFixture.Context.SaveChangesAsync();
        var stringContent = new StringContent($"AccountId,MeterReadingDateTime,MeterReadValue\r\n{entity.AccountId},{"22/04/2019 09:25"},{"01002"}\r\n{entity.AccountId},{"22/04/2019 09:25"},{"01002"}", MediaTypeHeaderValue.Parse("text/csv"));
        var response = await apiHostFixture.Client.PostAsync("meter-reading-uploads", stringContent);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<MeterReadingUploadResult>(responseBody);
        Assert.NotNull(result);
        Assert.Equal(1, result.NumberOfSuccessfulReadings);
        Assert.Equal(1, result.NumberOfFailedReadings);
    }
}