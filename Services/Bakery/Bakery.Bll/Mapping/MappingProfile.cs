using AutoMapper;
using Bakery.BLL.DTOs;
using Bakery.Domain.Entities;

namespace Bakery.BLL.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Клієнти
        CreateMap<Customer, CustomerDto>().ReverseMap();
        CreateMap<CustomerCreateDto, Customer>();
        CreateMap<CustomerUpdateDto, Customer>();

        // Замовлення
        CreateMap<Order, OrderResponseDto>()
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer != null ? src.Customer.FullName : null))
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

        CreateMap<CreateOrderDto, Order>()
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

        // Позиції замовлення
        CreateMap<OrderItem, OrderItemResponseDto>();
        CreateMap<CreateOrderItemDto, OrderItem>();

        // Оплати
        CreateMap<Payment, PaymentDto>().ReverseMap();
    }
}
