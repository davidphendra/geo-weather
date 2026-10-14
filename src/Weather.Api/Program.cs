using System.Diagnostics;

using Microsoft.AspNetCore.Mvc;

using Weather.Api.ErrorHandling;
using Weather.BuildingBlocks.Infrastructure;
using Weather.Modules.Favorites.Infrastructure;
using Weather.Modules.Geography.Infrastructure;
using Weather.Modules.Notes.Infrastructure;
using Weather.Modules.Weather.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    string traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
    context.ProblemDetails.Extensions.TryAdd("traceId", traceId);
    context.HttpContext.Response.Headers["X-Correlation-ID"] = traceId;
});
builder.Services.Configure<ApiBehaviorOptions>(options => options.InvalidModelStateResponseFactory = context =>
{
    ValidationProblemDetails problem = new(context.ModelState)
    {
        Type = ApiProblemTypes.MalformedRequest,
        Title = "Malformed request",
        Status = StatusCodes.Status400BadRequest,
        Detail = "The request could not be parsed.",
    };
    problem.Extensions["code"] = ApiErrorCodes.MalformedRequest;

    return new BadRequestObjectResult(problem);
});
builder.Services.AddInMemoryEventsBus();
builder.Services.AddWeatherModule(builder.Configuration);
builder.Services.AddFavoritesModule(builder.Configuration);   // migrate Favorites first
builder.Services.AddNotesModule(builder.Configuration);       // migrate Notes before the catalog publishes
builder.Services.AddGeographyModule(builder.Configuration);   // then publish the city catalog

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.MapControllers();

app.Run();

public partial class Program;
