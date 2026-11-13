using System.Diagnostics;

namespace Orders.BLL.Diagnostics;

public static class OrdersActivitySources
{
    public const string OrdersName = "Orders.Orders";

    public const string PaymentsName = "Orders.Payments";

    public static readonly ActivitySource Orders = new(OrdersName, "1.0.0");

    public static readonly ActivitySource Payments = new(PaymentsName, "1.0.0");

    public static string[] SourceNames { get; } = [OrdersName, PaymentsName];
}
