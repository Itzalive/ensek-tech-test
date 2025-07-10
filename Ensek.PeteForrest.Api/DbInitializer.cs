using System.Globalization;
using Ensek.PeteForrest.Domain;
using Ensek.PeteForrest.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using nietras.SeparatedValues;

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
        using (var csv = new Sep(',').Reader().FromFile(path))
        {
            foreach (var record in csv)
            {
                var accountId = record["AccountId"].Parse<int>();
                var firstName = record["FirstName"].ToString();
                var lastName = record["LastName"].ToString();
                context.Database.ExecuteSql(
                    $"INSERT INTO Accounts (AccountId, FirstName, LastName) values ({accountId}, '{firstName}', '{lastName}');");
            }
        }

        context.SaveChanges();
    }
}