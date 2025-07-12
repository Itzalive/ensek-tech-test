namespace Ensek.PeteForrest.Services.Infrastructure;

public interface IUnitOfWorkFactory
{
    IUnitOfWork Create();
}