namespace Bakery.Reviews.Domain.Exceptions;

public sealed class NotFoundException : DomainException
{
    public NotFoundException(string entityName, string key)
        : base($"{entityName} з ідентифікатором {key} не знайдено.")
    {
    }
}
