using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Taller.Datos;
using Taller.Datos.Repositorios;
using Taller.Negocio.Servicios;

var builder = WebApplication.CreateBuilder(args);

var conexion = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Configura ConnectionStrings:DefaultConnection.");
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtConfiguracion>()
    ?? throw new InvalidOperationException("Configura la sección Jwt.");
if (jwt.Clave.Length < 32 || string.IsNullOrWhiteSpace(jwt.Clave)
    || string.IsNullOrWhiteSpace(jwt.Emisor) || string.IsNullOrWhiteSpace(jwt.Audiencia)
    || jwt.Minutos <= 0)
{
    throw new InvalidOperationException("Configura Jwt:Clave (mínimo 32 caracteres), Emisor, Audiencia y Minutos.");
}

builder.Services.AddDbContext<TallerDbContext>(options => options.UseNpgsql(conexion));
builder.Services.AddScoped<IVehiculoRepositorio, VehiculoRepositorio>();
builder.Services.AddScoped<IOrdenTrabajoRepositorio, OrdenTrabajoRepositorio>();
builder.Services.AddScoped<IUsuarioRepositorio, UsuarioRepositorio>();
builder.Services.AddScoped<IVehiculoServicio, VehiculoServicio>();
builder.Services.AddScoped<IOrdenTrabajoServicio, OrdenTrabajoServicio>();
builder.Services.AddScoped<IAutenticacionServicio, AutenticacionServicio>();
builder.Services.AddSingleton(jwt);
builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(Taller.Negocio.MappingProfile).Assembly));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwt.Emisor,
        ValidateAudience = true,
        ValidAudience = jwt.Audiencia,
        ValidateLifetime = true,
        RequireExpirationTime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Clave)),
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        ClockSkew = TimeSpan.Zero,
        NameClaimType = JwtRegisteredClaimNames.Email,
        RoleClaimType = JwtConfiguracion.RolClaim
    };
});
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.OperationFilter<SwaggerAutorizacion>();
});

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddPolicy("PoliticaCors", policy => policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("PoliticaCors");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
