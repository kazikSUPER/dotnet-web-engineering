using AutoMapper;
using Bakery.BLL.DTOs.Customers;
using Bakery.BLL.DTOs.Orders;
using Bakery.BLL.DTOs.Payments;
using Bakery.Domain.Entities;

namespace Bakery.BLL.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Customer, CustomerResponseDto>().ForMember(x => x.FullName, x => x.MapFrom(y => y.FullName));
        CreateMap<Order, OrderResponseDto>().ForMember(x => x.CustomerName, x => x.MapFrom(y => y.Customer == null ? null : y.Customer.FullName));
        CreateMap<OrderItem, OrderItemResponseDto>();
        CreateMap<Payment, PaymentResponseDto>();
    }
}
