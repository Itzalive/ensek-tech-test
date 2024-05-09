using Ensek.PeteForrest.Domain;

namespace Ensek.PeteForrest.Services.Data
{
    public interface IAccountRepository
    {
        public Account Add(Account account);

        public Task<Account[]> GetAsync();

        public Task<Account?> GetAsync(int id);
    }
}
