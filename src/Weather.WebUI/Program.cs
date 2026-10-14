using Weather.WebUI.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddWeatherUi(builder.Configuration);
builder.Services.AddApiErrorHandling();
builder.AddModuleDatabases();
builder.Services.AddWeatherModules(builder.Configuration);

var app = builder.Build();

app.UseWeatherUiPipeline();
app.MapWeatherUiEndpoints();

app.Run();

public partial class Program;
