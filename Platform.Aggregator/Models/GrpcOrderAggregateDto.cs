namespace Platform.Aggregator.Models;

public sealed record GrpcOrderAggregateDto(
    int OrderId,
    string OrderNumber,
    int CustomerId,
    string CustomerName,
    string Status,
    decimal TotalAmount,
    List<GrpcOrderProductDto> Products,
    bool Cached,
    List<string> Warnings);

public sealed record GrpcOrderProductDto(
    int ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    GrpcCatalogProductSnapshot? Catalog,
    GrpcProductRatingSnapshot? Rating);

public sealed record GrpcCatalogProductSnapshot(
    string Name,
    decimal Price,
    int Stock,
    bool IsAvailable);

public sealed record GrpcProductRatingSnapshot(
    int TotalReviews,
    double AverageScore);
