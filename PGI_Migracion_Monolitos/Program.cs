using Microsoft.EntityFrameworkCore;
using PGI_Migracion_Monolitos.Data;
using PGI_Migracion_Monolitos.Interfaces.Repository;
using PGI_Migracion_Monolitos.Interfaces.Services;
using PGI_Migracion_Monolitos.Repository;
using PGI_Migracion_Monolitos.Services;

var builder = WebApplication.CreateBuilder(args);

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