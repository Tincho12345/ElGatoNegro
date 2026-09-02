using Microsoft.EntityFrameworkCore;
using TrabajosWeb.Api.Entities;
using TrabajosWeb.Shared.Helpers;

namespace TrabajosWeb.Api.Data;

public static class DbInitializer
{

    public static async Task SeedAsync(AppDbContext context, IConfiguration config, ILogger logger)
    {
        if (await context.Usuarios.AnyAsync())
            return;

        var usuario = config["AdminSeed:Usuario"];
        var email = config["AdminSeed:Email"];
        var password = config["AdminSeed:Password"];

        if (string.IsNullOrWhiteSpace(usuario) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "No se creó el usuario admin: falta configurar AdminSeed en user-secrets.");
            return;
        }

        context.Usuarios.Add(new Usuario
        {
            NombreUsuario = usuario,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Rol = "Admin",
            Activo = true
        });

        await context.SaveChangesAsync();
        logger.LogInformation("Usuario admin '{Usuario}' creado.", usuario);
    }

    public static async Task SeedCategoriasAsync(AppDbContext context, ILogger logger)
    {
        if (await context.Categorias.AnyAsync())
            return;

        var iniciales = new[]
        {
            ("Cortes", "fa-solid fa-scissors", 1),
            ("Color y tintura", "fa-solid fa-palette", 2),
            ("Brushing y peinados", "fa-solid fa-wind", 3),
            ("Tratamientos", "fa-solid fa-spa", 4),
            ("Peinados de fiesta", "fa-solid fa-crown", 5)
        };

        foreach (var (nombre, icono, orden) in iniciales)
        {
            context.Categorias.Add(new Categoria
            {
                Nombre = nombre,
                Slug = SlugHelper.Generar(nombre),
                Icono = icono,
                Orden = orden,
                Activa = true
            });
        }

        await context.SaveChangesAsync();
        logger.LogInformation("Se crearon {Cantidad} categorías iniciales.", iniciales.Length);
    }
}