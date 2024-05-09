using System.Globalization;
using CsvHelper;
using Ensek.PeteForrest.Domain;
using Ensek.PeteForrest.Services.Data;
using Microsoft.EntityFrameworkCore;

namespace Ensek.PeteForrest.Api;

public static class DbInitializer
{
    public static void Initialize(MeterContext context)
    {
        context.Database.EnsureCreated();

        if (context.Accounts.Any()) return;

        
        using var transaction = context.Database.BeginTransaction();
        try
        {
            context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT dbo.Accounts ON");
            context.SaveChanges();

            InsertAccountsFromCsv(context, "Data/Test_Accounts 2.csv");

            transaction.Commit();
        }
        catch(Exception)
        {
            transaction.Rollback();
            throw;
        }
        finally
        {
            context.Database.ExecuteSqlRaw("SET IDENTITY_INSERT dbo.Accounts OFF");
            context.SaveChanges();
        }
    }

    public static void InsertAccountsFromCsv(MeterContext context, string path)
    {
        using (var reader = new StreamReader(path))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            var records = csv.GetRecords<Account>();
            foreach (var record in records)
            {
                context.Database.ExecuteSql(
                    $"INSERT INTO Accounts (AccountId, FirstName, LastName) values ({record.AccountId}, {record.FirstName}, {record.LastName});");
            }
        }
        context.SaveChanges();
    }
}