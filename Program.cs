using CooperativaApp.Data;
using CooperativaApp.Interfaces;
using CooperativaApp.Models;
using CooperativaApp.Repositories;
using CooperativaApp.Services;
using CooperativaApp.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
// 🛡️ 1. CONFIGURACIÓN JWT DESDE APPSETTINGS
// Extraemos la sección completa para mayor escalabilidad

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory()) // Asegura la ruta raíz
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true) // Por si acaso
    .AddEnvironmentVariables();
var jwtSection = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSection["SecretKey"] ?? throw new InvalidOperationException("Falta 'SecretKey' en appsettings.json");
if (string.IsNullOrEmpty(secretKey))
{
    throw new InvalidOperationException("🚨 ERROR CRÍTICO: 'SecretKey' no se pudo leer del appsettings.json. Verifique el nombre y la ubicación del archivo.");
}
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),

        // Configuración lista para Producción (Cámbialos a true cuando definas Issuer/Audience en el JSON)
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = "CooperativaApp", // 👈 Debe decir exactamente esto
        ValidAudience = "CooperativaApp_Audience", // 👈 Debe decir exactamente esto
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,

        // Mapeo Galáctico para Roles
        RoleClaimType = "role",
        NameClaimType = "unique_name"
    };
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine("💥 FALLO DE AUTENTICACIÓN: " + context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine("✅ TOKEN VALIDADO CORRECTAMENTE");
            return Task.CompletedTask;
        }
    };
});

// 🔹 2. REGISTRO DE SERVICIOS (Inyección de Dependencias)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IPagoService, PagoService>();
builder.Services.AddScoped<ISolicitudPagoService, SolicitudPagoService>();
builder.Services.AddScoped<ICreditoService, CreditoService>();
builder.Services.AddScoped<ISolicitudService, SolicitudService>();
builder.Services.AddScoped<IFamiliaridadService, FamiliaridadService>();
builder.Services.AddMemoryCache();

// 🧬 3. CONTROLADORES Y JSON
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy",
        policy =>
        {
            policy.WithOrigins(
                "https://cooperativa.mandersystems.com",
              "http://localhost:5173"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
        });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IAporteService, AporteService>();
builder.Services.AddEndpointsApiExplorer();

// 💾 4. BASE DE DATOS
builder.Services.AddDbContext<CooperativaContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 🌐 5. CORS PRO
/*builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Authorization");
    });
});
*/
var app = builder.Build();
app.UseDeveloperExceptionPage();
app.Use(async (context, next) =>
{
    Console.WriteLine($"➡️ {context.Request.Method} {context.Request.Path}");
    await next();
    Console.WriteLine($"⬅️ {context.Response.StatusCode}");
});
app.UseSwagger();
app.UseSwaggerUI();
// 1. Definimos la ruta física
var resourcesPath = Path.Combine(builder.Environment.ContentRootPath, "Resources");

// 🛡️ VERIFICACIÓN TITANIUM: Si no existe, la creamos en el acto
if (!Directory.Exists(resourcesPath))
{
    Directory.CreateDirectory(resourcesPath);
    Directory.CreateDirectory(Path.Combine(resourcesPath, "Vouchers")); // Subcarpeta para los vouchers
}

// 2. Ahora sí, configuramos los archivos estáticos sin riesgo de caída
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(resourcesPath),
    RequestPath = "/Resources"
});
// 🔹 6. MIDDLEWARE (EL ORDEN SAGRADO)

app.UseRouting();

app.UseCors("FrontendPolicy");

app.Use(async (context, next) =>
{
    if (context.Request.Method == "OPTIONS")
    {
        context.Response.Headers.Append("Access-Control-Allow-Origin", "https://cooperativa.mandersystems.com");
        context.Response.Headers.Append("Access-Control-Allow-Headers", "Origin, X-Requested-With, Content-Type, Accept, Authorization");
        context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
        context.Response.StatusCode = 200;
        await context.Response.CompleteAsync();
        return;
    }

await next();

});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


app.Run();