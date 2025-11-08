using Microsoft.EntityFrameworkCore;
using RefactorizacionService.Data;
using RefactorizacionService.Services;

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<RefactorizacionDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Services
builder.Services.AddScoped<IAnalizadorDependenciasService, AnalizadorDependenciasService>();
builder.Services.AddScoped<IGeneradorClientesHttpService, GeneradorClientesHttpService>();
builder.Services.AddScoped<IRefactorizadorCodigoService, RefactorizadorCodigoService>();
// Agrega estas líneas después de los servicios existentes
builder.Services.AddScoped<IServiciosMigradosService, ServiciosMigradosService>();
builder.Services.AddScoped<IRefactorizacionHistorialService, RefactorizacionHistorialService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();