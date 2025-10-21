using ContainerizationService.Data;
using ContainerizationService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configurar DbContext
builder.Services.AddDbContext<ContainerizationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Registrar servicios
builder.Services.AddScoped<IDockerService, DockerService>();
builder.Services.AddScoped<IDockerComposeService, DockerComposeService>();
builder.Services.AddScoped<IPortManagementService, PortManagementService>();

// Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Containerization Service API",
        Version = "v1",
        Description = "API para contenerizar y desplegar microservicios generados"
    });
});

var app = builder.Build();

// Aplicar migraciones automáticamente en desarrollo
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ContainerizationDbContext>();
        try
        {
            dbContext.Database.Migrate();
            Console.WriteLine("? Migraciones aplicadas exitosamente");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Error al aplicar migraciones: {ex.Message}");
        }
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Containerization Service API v1");
        c.RoutePrefix = string.Empty; // Swagger en la raíz
    });
}

app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

Console.WriteLine("?? Containerization Service iniciado");
Console.WriteLine($"?? Swagger UI disponible en: http://localhost:{builder.Configuration.GetValue<int>("Port", 5000)}");

app.Run();