using System.Net;
using Ensek.PeteForrest.Domain;
using Newtonsoft.Json;
using Xunit;

namespace Ensek.PeteForrest.Api.Tests {
    public class AccountControllerTests(ApiHostFixture apiHostFixture) : IClassFixture<ApiHostFixture>
    {
        [Fact]
        public async Task CanReturnAccountsList()
        {
            var response = await apiHostFixture.Client.GetAsync("Account");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseString = await response.Content.ReadAsStringAsync();
            var accounts = JsonConvert.DeserializeObject<Account[]>(responseString);
            Assert.NotNull(accounts);
            Assert.NotEmpty(accounts);
            Assert.Equal(27, accounts.Length);
        }
    }
}