using AutoMapper;
using TrabajosWeb.Api.Entities;
using TrabajosWeb.Shared.DTOs;

namespace TrabajosWeb.Api.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Trabajo, TrabajoDto>()
            .ForMember(d => d.CategoriaNombre,
                o => o.MapFrom(s => s.Categoria != null ? s.Categoria.Nombre : string.Empty))
            .ForMember(d => d.CategoriaSlug,
                o => o.MapFrom(s => s.Categoria != null ? s.Categoria.Slug : string.Empty));

        CreateMap<TrabajoCreateDto, Trabajo>();

        CreateMap<Media, MediaDto>()
            .ForMember(d => d.Url, o => o.MapFrom(s => s.RutaRelativa));

        CreateMap<Consulta, ConsultaDto>();
        CreateMap<ConsultaCreateDto, Consulta>();

        CreateMap<Categoria, CategoriaDto>()
            .ForMember(d => d.CantidadTrabajos, o => o.MapFrom(s => s.Trabajos.Count));

        // El slug lo calcula el controller, no se mapea
        CreateMap<CategoriaCreateDto, Categoria>()
            .ForMember(d => d.Slug, o => o.Ignore());

        CreateMap<AjustesSitio, AjustesSitioDto>();
        CreateMap<AjustesSitioUpdateDto, AjustesSitio>();
    }
}