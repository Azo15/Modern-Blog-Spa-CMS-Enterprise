using AutoMapper;
using BusinessLayer.Dtos;
using EntityLayer.Concrete;

namespace BusinessLayer.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Blog Mappings
            CreateMap<Blog, BlogDto>()
                .ForMember(dest => dest.CatagoryTitle, opt => opt.MapFrom(src => src.Catagory.Title))
                .ForMember(dest => dest.AppUserFullName, opt => opt.MapFrom(src => $"{src.AppUser.FirstName} {src.AppUser.LastName}"));
            
            CreateMap<BlogCreateDto, Blog>();
            CreateMap<BlogUpdateDto, Blog>().ReverseMap();

            // Catagory Mappings
            CreateMap<Catagory, CatagoryDto>().ReverseMap();
            CreateMap<CatagoryCreateDto, Catagory>();
            CreateMap<CatagoryUpdateDto, Catagory>().ReverseMap();
        }
    }
}
