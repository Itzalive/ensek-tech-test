using Ensek.PeteForrest.Infrastructure.Data;
using Ensek.PeteForrest.Services.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Ensek.PeteForrest.Infrastructure.Behaviours
{
    internal sealed class UnitOfWorkFactory(IServiceProvider serviceProvider) : IUnitOfWorkFactory
    {
        public IUnitOfWork Create()
        {
            var meterContext = serviceProvider.GetRequiredService<MeterContext>();
            var transaction = meterContext.Database.BeginTransaction();
            return new UnitOfWork(meterContext, transaction);
        }

        public sealed class UnitOfWork(MeterContext meterContext, IDbContextTransaction transaction) : IUnitOfWork
        {
            private bool _isRollingBack;

            public async Task RollbackAsync()
            {
                _isRollingBack = true;
                await transaction.RollbackAsync();
                await transaction.DisposeAsync();
            }

            public void Dispose()
            {
                if (_isRollingBack) return;
                meterContext.SaveChanges();
                transaction.Commit();
                transaction.Dispose();
            }

            public async ValueTask DisposeAsync()
            {
                if (_isRollingBack) return;

                await meterContext.SaveChangesAsync();
                await transaction.CommitAsync();
                await transaction.DisposeAsync();
            }
        }
    }
}
