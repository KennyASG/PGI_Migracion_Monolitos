using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using PGI_Migracion_Monolitos.Data;
using PGI_Migracion_Monolitos.Interfaces.Repository;
using PGI_Migracion_Monolitos.Interfaces.Services;
using PGI_Migracion_Monolitos.Repository;
using PGI_Migracion_Monolitos.Services;

var builder = WebApplication.CreateBuilder(args);

// Aumentar el límite para requests grandes (ej: 200 MB)
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 200_000_000; // 200 MB
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 200_000_000; // 200 MB
});

// Agrega los servicios al contenedor
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Inyección de dependencias personalizada
builder.Services.AddScoped<IDependenciaRepository, DependenciaRepository>();
builder.Services.AddScoped<IAnalizadorDependenciasService, AnalizadorDependenciasService>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configura el pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.UseCors();
app.MapControllers();

app.Run();
