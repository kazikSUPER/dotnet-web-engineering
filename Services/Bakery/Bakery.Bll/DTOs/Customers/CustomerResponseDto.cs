namespace Bakery.BLL.DTOs.Customers;

public sealed class CustomerResponseDto
{
    public int Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public char Gender { get; init; }
    public int Age { get; init; }
    public DateTime CreatedAt { get; init; }
}
