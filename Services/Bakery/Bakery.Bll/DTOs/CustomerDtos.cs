using System.ComponentModel.DataAnnotations;

namespace Bakery.BLL.DTOs;

public class CustomerDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Phone { get => PhoneNumber; set => PhoneNumber = value; }
    public char Gender { get; set; }
    public int Age { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CustomerCreateDto
{
    [Required(ErrorMessage = "Ім'я є обов'язковим для заповнення")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Ім'я повинно містити від 2 до 50 символів")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Прізвище є обов'язковим для заповнення")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Прізвище повинно містити від 2 до 50 символів")]
    public string LastName { get; set; } = string.Empty;

    public string FullName
    {
        get => $"{FirstName} {LastName}".Trim();
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                var parts = value.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                FirstName = parts[0];
                if (parts.Length > 1) LastName = parts[1];
            }
        }
    }

    [Required(ErrorMessage = "Email є обов'язковим")]
    [EmailAddress(ErrorMessage = "Некоректний формат електронної пошти")]
    [StringLength(100, ErrorMessage = "Email не повинен перевищувати 100 символів")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Номер телефону є обов'язковим")]
    [Phone(ErrorMessage = "Некоректний формат номера телефону")]
    [StringLength(20, MinimumLength = 10, ErrorMessage = "Телефон повинен містити від 10 до 20 символів")]
    public string PhoneNumber { get; set; } = string.Empty;

    public string Phone { get => PhoneNumber; set => PhoneNumber = value; }

    [Required]
    [RegularExpression("^[MFOmfo]$", ErrorMessage = "Стать повинна бути M, F або O")]
    public char Gender { get; set; } = 'M';

    [Range(14, 120, ErrorMessage = "Вік повинен бути від 14 до 120 років")]
    public int Age { get; set; } = 18;
}

public class CustomerUpdateDto
{
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    [Range(14, 120)]
    public int Age { get; set; }
}

public class CustomerSummaryDto
{
    public int CustomerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime? LastOrderDate { get; set; }
}
