using Bakery.Domain.Exceptions;

namespace Bakery.Domain.Entities;

public class Customer
{
    private Customer() { }

    public Customer(string firstName, string lastName, string email, string phoneNumber, char gender, int age)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new DomainValidationException("Ім'я клієнта є обов'язковим.");
        if (string.IsNullOrWhiteSpace(lastName)) throw new DomainValidationException("Прізвище клієнта є обов'язковим.");
        if (string.IsNullOrWhiteSpace(email)) throw new DomainValidationException("Email клієнта є обов'язковим.");
        if (age is < 14 or > 120) throw new DomainValidationException("Вік клієнта має бути від 14 до 120 років.");
        if (!"MFO".Contains(char.ToUpperInvariant(gender))) throw new DomainValidationException("Стать має бути M, F або O.");
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = email.Trim().ToLowerInvariant();
        PhoneNumber = phoneNumber.Trim();
        Gender = char.ToUpperInvariant(gender);
        Age = age;
        CreatedAt = DateTime.UtcNow;
    }

    public int Id { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Email { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public char Gender { get; private set; }
    public int Age { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public ICollection<Order> Orders { get; private set; } = new List<Order>();

    public void Update(string firstName, string lastName, string email, string phoneNumber, int age)
    {
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName)) throw new DomainValidationException("Ім'я та прізвище є обов'язковими.");
        if (age is < 14 or > 120) throw new DomainValidationException("Вік клієнта має бути від 14 до 120 років.");
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = email.Trim().ToLowerInvariant();
        PhoneNumber = phoneNumber.Trim();
        Age = age;
    }

    public void Delete() => IsDeleted = true;
}
