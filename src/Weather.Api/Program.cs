using Weather.Api.ErrorHandling;
using Weather.Modules.Geography.Infrastructure;
using Weather.Modules.Weather.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddWeatherModule(builder.Configuration);
builder.Services.AddGeographyModule(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.MapControllers();

app.Run();

public partial class Program;
