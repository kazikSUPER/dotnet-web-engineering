using AutoMapper;
using Bakery.BLL.DTOs.Customers;
using Bakery.BLL.DTOs.Orders;
using Bakery.BLL.DTOs.Payments;
using Bakery.BLL.DTOs.Products;
using Bakery.Domain.Entities;

namespace Bakery.BLL.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Customer, CustomerResponseDto>().ForMember(x => x.FullName, x => x.MapFrom(y => y.FullName));
        CreateMap<Order, OrderResponseDto>().ForMember(x => x.CustomerName, x => x.MapFrom(y => y.Customer == null ? null : y.Customer.FullName));
        CreateMap<OrderItem, OrderItemResponseDto>();
        CreateMap<OrderStatusHistory, OrderStatusHistoryDto>();
        CreateMap<Payment, PaymentResponseDto>();
        CreateMap<ProductCategory, ProductInCategoryDto>()
            .ForMember(x => x.ProductName, x => x.MapFrom(y => y.Product == null ? string.Empty : y.Product.Name))
            .ForMember(x => x.Price, x => x.MapFrom(y => y.Product == null ? 0m : y.Product.Price))
            .ForMember(x => x.CategoryName, x => x.MapFrom(y => y.Category == null ? string.Empty : y.Category.Name));
    }
}
