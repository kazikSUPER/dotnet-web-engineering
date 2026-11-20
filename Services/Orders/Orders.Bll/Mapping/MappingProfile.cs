using AutoMapper;
using Orders.BLL.DTOs.Customers;
using Orders.BLL.DTOs.Orders;
using Orders.BLL.DTOs.Payments;
using Orders.Domain.Entities;

namespace Orders.BLL.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Customer, CustomerResponseDto>().ForMember(x => x.FullName, x => x.MapFrom(y => y.FullName));
        CreateMap<Order, OrderResponseDto>().ForMember(x => x.CustomerName, x => x.MapFrom(y => y.Customer == null ? null : y.Customer.FullName));
        CreateMap<OrderItem, OrderItemResponseDto>();
        CreateMap<OrderStatusHistory, OrderStatusHistoryDto>();
        CreateMap<Payment, PaymentResponseDto>();
    }
}
