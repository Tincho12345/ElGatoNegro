using System.ComponentModel.DataAnnotations;

namespace TrabajosWeb.Api.Entities;

/// <summary>
/// Base de toda entidad persistida: Id Guid y campos de auditoría.
/// Los campos se completan solos en AppDbContext.SaveChangesAsync.
/// </summary>
public abstract class EntidadAuditable
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    [StringLength(80)]
    public string? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    [StringLength(80)]
    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }
}