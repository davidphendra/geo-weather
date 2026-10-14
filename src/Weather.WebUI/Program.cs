using Weather.WebUI.Hosting;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment())
{
    // Machine-readable console logs outside Development (no secrets are logged).
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole();
}

builder.Services.AddWeatherUi(builder.Configuration, builder.Environment);
builder.Services.AddApiErrorHandling();
builder.AddModuleDatabases();
builder.Services.AddWeatherModules(builder.Configuration);

var app = builder.Build();

app.UseWeatherUiPipeline();
app.MapWeatherUiEndpoints();

app.Run();

public partial class Program;
