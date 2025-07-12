namespace Ensek.PeteForrest.Services.Infrastructure;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    Task RollbackAsync();
}