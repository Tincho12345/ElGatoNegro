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
                o => o.MapFrom(s => s.Categoria != null ? s.Categoria.Slug : string.Empty))
            .ForMember(d => d.SubcategoriaNombre,
                o => o.MapFrom(s => s.Subcategoria != null ? s.Subcategoria.Nombre : null))
            .ForMember(d => d.SubcategoriaSlug,
                o => o.MapFrom(s => s.Subcategoria != null ? s.Subcategoria.Slug : null))
            .ForMember(d => d.MarcaNombre,
                o => o.MapFrom(s => s.Marca != null ? s.Marca.Nombre : null))
            .ForMember(d => d.MarcaSlug,
                o => o.MapFrom(s => s.Marca != null ? s.Marca.Slug : null));

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

        CreateMap<Subcategoria, SubcategoriaDto>()
            .ForMember(d => d.CantidadProductos, o => o.MapFrom(s => s.Trabajos.Count));

        CreateMap<SubcategoriaCreateDto, Subcategoria>()
            .ForMember(d => d.Slug, o => o.Ignore());

        CreateMap<Marca, MarcaDto>()
            .ForMember(d => d.CantidadProductos, o => o.MapFrom(s => s.Trabajos.Count));

        CreateMap<MarcaCreateDto, Marca>()
            .ForMember(d => d.Slug, o => o.Ignore());

        CreateMap<AjustesSitio, AjustesSitioDto>();
        CreateMap<AjustesSitioUpdateDto, AjustesSitio>();
    }
}