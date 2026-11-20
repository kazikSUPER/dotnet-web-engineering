using Bakery.Reviews.Domain.Common;

namespace Bakery.Reviews.Domain.Exceptions;

public class ConflictException : DomainException
{
    public ConflictException(string message)
        : base(message)
    {
    }

    public static ConflictException Concurrency(BaseEntity entity, int expectedVersion)
    {
        return new ConflictException(
            $"{entity.GetType().Name} {entity.Id} застарів: очікувана версія {expectedVersion}, запис уже змінено.");
    }
}
