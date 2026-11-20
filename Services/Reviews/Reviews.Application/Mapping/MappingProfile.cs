using AutoMapper;
using Bakery.Reviews.Application.DTOs;
using Bakery.Reviews.Domain.Entities;
using Bakery.Reviews.Domain.ValueObjects;

namespace Bakery.Reviews.Application.Mapping;

public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Rating, RatingDto>();
        CreateMap<AdminResponse, AdminResponseDto>();

        CreateMap<Review, ReviewDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.CustomerName.Value))
            .ForMember(dest => dest.Comment, opt => opt.MapFrom(src => src.Comment.Text))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<Review, ReviewListItemDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.CustomerName.Value))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<ProductRating, ProductRatingDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()));

        CreateMap<ModerationLog, ModerationLogDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.ToString()))
            .ForMember(dest => dest.ReviewId, opt => opt.MapFrom(src => src.ReviewId.ToString()))
            .ForMember(dest => dest.Action, opt => opt.MapFrom(src => src.Action.ToString()));
    }
}
