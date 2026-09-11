using Microsoft.EntityFrameworkCore;
using TrabajosWeb.Api.Entities;
using TrabajosWeb.Api.Services;
using TrabajosWeb.Shared.Helpers;

namespace TrabajosWeb.Api.Data;

public class AppDbContext : DbContext
{
    private readonly IUsuarioActualService? _usuarioActual;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IUsuarioActualService? usuarioActual = null) : base(options)
    {
        _usuarioActual = usuarioActual;
    }

    public DbSet<Trabajo> Trabajos => Set<Trabajo>();
    public DbSet<Media> Medios => Set<Media>();
    public DbSet<Consulta> Consultas => Set<Consulta>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Subcategoria> Subcategorias => Set<Subcategoria>();
    public DbSet<Marca> Marcas => Set<Marca>();
    public DbSet<AjustesSitio> AjustesSitio => Set<AjustesSitio>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Media>()
            .HasOne(m => m.Trabajo)
            .WithMany(t => t.Medios)
            .HasForeignKey(m => m.TrabajoId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Trabajo>()
            .HasIndex(t => t.Publicado);

        modelBuilder.Entity<Consulta>()
            .HasIndex(c => c.CreatedDate);

        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.NombreUsuario)
            .IsUnique();

        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<RefreshToken>()
            .HasOne(r => r.Usuario)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(r => r.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(r => r.Token);

        modelBuilder.Entity<Trabajo>()
            .HasOne(t => t.Categoria)
            .WithMany(c => c.Trabajos)
            .HasForeignKey(t => t.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Categoria>()
            .HasIndex(c => c.Slug)
            .IsUnique();

        modelBuilder.Entity<Categoria>()
            .HasIndex(c => c.Orden);

        // Subcategoría y marca son opcionales: borrarlas nunca arrastra trabajos,
        // por eso Restrict. El panel avisa si todavía tienen productos asociados.
        modelBuilder.Entity<Trabajo>()
            .HasOne(t => t.Subcategoria)
            .WithMany(s => s.Trabajos)
            .HasForeignKey(t => t.SubcategoriaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Trabajo>()
            .HasOne(t => t.Marca)
            .WithMany(m => m.Trabajos)
            .HasForeignKey(t => t.MarcaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Subcategoria>()
            .HasIndex(s => s.Slug)
            .IsUnique();

        modelBuilder.Entity<Subcategoria>()
            .HasIndex(s => s.Orden);

        modelBuilder.Entity<Marca>()
            .HasIndex(m => m.Slug)
            .IsUnique();

        modelBuilder.Entity<Marca>()
            .HasIndex(m => m.Orden);

        // El precio se filtra por rango en la galería de productos.
        modelBuilder.Entity<Trabajo>()
            .HasIndex(t => t.Precio);

        modelBuilder.Entity<RefreshToken>()
            .Ignore(r => r.EstaActivo);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AplicarAuditoria();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        AplicarAuditoria();
        return base.SaveChanges();
    }

    /// <summary>
    /// Completa CreatedBy/CreatedDate y ModifiedBy/ModifiedDate en un solo lugar,
    /// para que ningún controller tenga que acordarse de hacerlo.
    /// </summary>
    private void AplicarAuditoria()
    {
        var ahora = TimeHelper.AhoraUtc;
        var usuario = _usuarioActual?.NombreUsuario ?? "sistema";

        foreach (var entry in ChangeTracker.Entries<EntidadAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity.Id == Guid.Empty)
                        entry.Entity.Id = Guid.CreateVersion7();

                    entry.Entity.CreatedDate = ahora;
                    entry.Entity.CreatedBy = usuario;
                    break;

                case EntityState.Modified:
                    entry.Entity.ModifiedDate = ahora;
                    entry.Entity.ModifiedBy = usuario;

                    // Nadie puede reescribir quién creó el registro
                    entry.Property(e => e.CreatedDate).IsModified = false;
                    entry.Property(e => e.CreatedBy).IsModified = false;
                    break;
            }
        }
    }
}