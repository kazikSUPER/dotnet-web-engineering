using System.ComponentModel.DataAnnotations;

namespace Bakery.BLL.DTOs.Customers;

public sealed class UpdateCustomerRequest
{
    [Required, StringLength(50, MinimumLength = 2)] public string FirstName { get; init; } = string.Empty;
    [Required, StringLength(50, MinimumLength = 2)] public string LastName { get; init; } = string.Empty;
    [Required, EmailAddress, StringLength(100)] public string Email { get; init; } = string.Empty;
    [Required, Phone, StringLength(20, MinimumLength = 10)] public string PhoneNumber { get; init; } = string.Empty;
    [Range(14, 120)] public int Age { get; init; }
}
