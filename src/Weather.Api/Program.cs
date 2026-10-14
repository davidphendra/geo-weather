using global::Weather.Api.ErrorHandling;
using global::Weather.Api.Modules.Weather;
using global::Weather.Modules.Weather.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddWeatherModule(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.MapWeatherEndpoints();

app.Run();

public partial class Program;
