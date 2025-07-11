using Ensek.PeteForrest.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using nietras.SeparatedValues;

namespace Ensek.PeteForrest.Db.Creater;

public static class AccountSeeder
{
    public static async Task<bool> SeedAccountsAsync(MeterContext context, string csvPath)
    {
        if (await context.Accounts.AnyAsync()) return false;
        
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT dbo.Accounts ON");
            await context.SaveChangesAsync();

            await InsertAccountsFromCsvAsync(context, csvPath);

            await transaction.CommitAsync();
        }
        catch(Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            await context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT dbo.Accounts OFF");
            await context.SaveChangesAsync();
        }

        return true;
    }

    public static async Task InsertAccountsFromCsvAsync(MeterContext context, string path)
    {
        using (var csv = await new Sep(',').Reader().FromFileAsync(path))
        {
            await foreach (var record in csv)
            {
                var accountId = record["AccountId"].Parse<int>();
                var firstName = record["FirstName"].ToString();
                var lastName = record["LastName"].ToString();
                await context.Database.ExecuteSqlAsync(
                    $"INSERT INTO Accounts (AccountId, FirstName, LastName) values ({accountId}, '{firstName}', '{lastName}');");
            }
        }

        await context.SaveChangesAsync();
    }
}