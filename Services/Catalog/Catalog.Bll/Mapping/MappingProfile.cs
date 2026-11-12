using AutoMapper;
using Catalog.BLL.DTOs.Products;
using Catalog.Domain.Entities;

namespace Catalog.BLL.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<ProductCategory, ProductInCategoryDto>()
            .ForMember(x => x.ProductName, x => x.MapFrom(y => y.Product == null ? string.Empty : y.Product.Name))
            .ForMember(x => x.Price, x => x.MapFrom(y => y.Product == null ? 0m : y.Product.Price))
            .ForMember(x => x.CategoryName, x => x.MapFrom(y => y.Category == null ? string.Empty : y.Category.Name));
    }
}
