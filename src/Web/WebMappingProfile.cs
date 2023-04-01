using AutoMapper;
using Microsoft.eShopWeb.ApplicationCore.DTO;
using Microsoft.eShopWeb.Web.Pages.Basket;

public class WebMappingProfile : Profile
{
    public WebMappingProfile()
    {
        CreateMap<BasketItemViewModel, DeliveryItemDto>()
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.ProductName))
            .ForMember(dest => dest.UnitPrice, opt => opt.MapFrom(src => src.UnitPrice))
            .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity));

        CreateMap<IEnumerable<BasketItemViewModel>, DeliveryDetailsDto>()
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src))
            .ForMember(dest => dest.DeliveryAddress, opt => opt.Ignore());
    }
}
