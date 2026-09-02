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