using AutoMapper;
using Optsol.Components.Application.DataTransferObjects;
using Optsol.Components.Test.Utils.Entity.Entities;
using Optsol.Components.Test.Utils.ViewModels;

namespace Optsol.Components.Test.Utils.Mapper
{
    public class TestEntityToViewModel : Profile
    {
        public TestEntityToViewModel()
        {
            CreateMap<TestEntity, TestResponseDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(source => source.Id.ToString()))
                .ForMember(dest => dest.Nome, opt => opt.MapFrom(source => source.Nome.ToString()))
                .ForMember(dest => dest.Contato, opt => opt.MapFrom(source => source.Email.ToString()))
                .ForMember(dest => dest.Ativo, opt => opt.MapFrom(source => source.Ativo ? "Ativo" : "Inativo"));

            CreateMap<TestEntity, TestRequestDto>()
               .ForMember(dest => dest.Nome, opt => opt.MapFrom(source => source.Nome.ToString()))
               .ForMember(dest => dest.Contato, opt => opt.MapFrom(source => source.Email.ToString()));

            CreateMap<TestEntity, BaseModel>();
        }
    }
}
