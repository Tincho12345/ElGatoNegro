using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using TrabajosWeb.Web.Configuration;
using TrabajosWeb.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- Configuración tipada ----------
builder.Services.Configure<ApiSettings>(
    builder.Configuration.GetSection(ApiSettings.SectionName));

builder.Services.Configure<ContactoSettings>(
    builder.Configuration.GetSection(ContactoSettings.SectionName));

var apiSettings = builder.Configuration
    .GetSection(ApiSettings.SectionName).Get<ApiSettings>()
    ?? throw new InvalidOperationException("Falta la sección ApiSettings.");

// ---------- Detrás del túnel de Cloudflare ----------
// El túnel entrega HTTP plano a localhost; estas cabeceras le devuelven a la app
// el esquema y la IP reales del visitante.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // El proxy es cloudflared en la misma máquina, así que no hace falta
    // restringir por red de origen.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// ---------- Cliente HTTP ----------
builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
{
    client.BaseAddress = new Uri(apiSettings.BaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5); // las subidas de video tardan
});

// ---------- Autenticación por cookie ----------
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Cuenta/Login";
        options.LogoutPath = "/Cuenta/Logout";
        options.AccessDeniedPath = "/Cuenta/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.Name = "TrabajosWeb.Auth";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SoloAdmin", policy => policy.RequireRole("Admin"));
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Va primero: el resto del pipeline necesita ver el esquema real.
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // El TLS y HSTS los maneja Cloudflare, no la app.
}

// Sin UseHttpsRedirection: en local corrés por HTTP plano (5275)
// y en producción el TLS lo termina Cloudflare.

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();