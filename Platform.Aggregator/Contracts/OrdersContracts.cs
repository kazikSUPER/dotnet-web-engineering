namespace Platform.Aggregator.Contracts;

public sealed record OrderContract
{
    public int Id { get; init; }

    public string OrderNumber { get; init; } = string.Empty;

    public int CustomerId { get; init; }

    public string? CustomerName { get; init; }

    public string Status { get; init; } = string.Empty;

    public decimal TotalAmount { get; init; }

    public DateTime OrderDate { get; init; }

    public string? Notes { get; init; }

    public OrderItemContract[] Items { get; init; } = [];
}

public sealed record OrderItemContract
{
    public int Id { get; init; }

    public int ProductId { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public decimal UnitPrice { get; init; }

    public int Quantity { get; init; }

    public decimal TotalPrice { get; init; }
}

public sealed record CustomerContract
{
    public int Id { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string PhoneNumber { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }
}

public sealed record PaymentContract
{
    public int OrderId { get; init; }

    public string TransactionNumber { get; init; } = string.Empty;

    public string PaymentMethod { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public DateTime PaymentDate { get; init; }

    public string Status { get; init; } = string.Empty;
}

public sealed record OrderStatusHistoryContract
{
    public int Id { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTime ChangedAt { get; init; }

    public string? Comment { get; init; }
}
