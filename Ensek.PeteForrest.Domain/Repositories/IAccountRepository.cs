using Ensek.PeteForrest.Domain;

namespace Ensek.PeteForrest.Domain.Repositories
{
    public interface IAccountRepository
    {
        public Account Add(Account account);

        public Task<Account[]> GetAsync();

        public Task<Account?> GetAsync(int id);

        public Task<Account[]> GetAsync(IEnumerable<int> ids);
    }
}
