using Flatplanner.Api;
using Flatplanner.Api.Endpoints;
using Flatplanner.Api.Listings;
using Flatplanner.Core.Data;
using Flatplanner.Core.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<FlatplannerOptions>()
    .BindConfiguration("")
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.DefaultIgnoreCondition = ApiJson.Options.DefaultIgnoreCondition);
builder.Services.AddProblemDetails();

builder.Services.AddDbContext<FlatplannerDbContext>((services, db) =>
{
    var settings = services.GetRequiredService<IOptions<FlatplannerOptions>>().Value;
    db.UseMySql(settings.ConnectionString, DbConstants.ServerVersion, mysql => mysql.EnableRetryOnFailure(5));
});
builder.Services.AddScoped<MetaService>();
builder.Services.AddScoped<ListingsService>();

var app = builder.Build();

if (app.Services.GetRequiredService<IOptions<FlatplannerOptions>>().Value.DbMigrateOnStartup)
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<FlatplannerDbContext>().Database.Migrate();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapHealth();
app.MapMeta();
app.MapListings();
app.Map("/api/{**rest}", () => Results.Problem(statusCode: StatusCodes.Status404NotFound));
app.MapFallbackToFile("index.html");

app.Run();
