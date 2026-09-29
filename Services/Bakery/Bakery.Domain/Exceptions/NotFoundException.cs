namespace Bakery.Domain.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string entityName, object key)
        : base($"Сутність '{entityName}' з ідентифікатором ({key}) не знайдена.")
    {
    }
}
