namespace DomainObjects.Data;

public interface IUnitOfWork
{
    Task<bool> Commit();
}
