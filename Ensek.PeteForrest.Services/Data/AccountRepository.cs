using Ensek.PeteForrest.Domain;
using Microsoft.EntityFrameworkCore;

namespace Ensek.PeteForrest.Services.Data;

public class AccountRepository(MeterContext context) : IAccountRepository
{
    private readonly MeterContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public Account Add(Account account) => _context.Accounts.Add(account).Entity;

    public Task<Account[]> GetAsync() => _context.Accounts
        .ToArrayAsync();

    public Task<Account?> GetAsync(int id) => _context.Accounts
        .Include(a => a.MeterReadings)
        .Where(a => a.AccountId == id)
        .SingleOrDefaultAsync();
}