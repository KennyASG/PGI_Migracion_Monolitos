using TransformadorService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<ITransformadorService, TransformadorService.Services.TransformadorService>();
builder.Services.AddScoped<IAnalizadorCodigoService, AnalizadorCodigoService>();
builder.Services.AddScoped<IGeneradorMicroservicioService, GeneradorMicroservicioService>();

builder.Services.AddScoped<IAnalizadorFuncionalService, AnalizadorFuncionalService>();
builder.Services.AddScoped<IGeneradorFuncionalService, GeneradorFuncionalService>();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();