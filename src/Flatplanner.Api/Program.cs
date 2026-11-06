using System.Collections.Concurrent;
using Flatplanner.Api;
using Flatplanner.Api.Chat;
using Flatplanner.Api.Endpoints;
using Flatplanner.Api.Listings;
using Flatplanner.Core.Data;
using Flatplanner.Core.Llm;
using Flatplanner.Core.Options;
using Flatplanner.Core.Search;
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

builder.Services.AddSingleton(services =>
{
    var settings = services.GetRequiredService<IOptions<FlatplannerOptions>>().Value;
    return new IntentInterpreter(
        new LlmJsonClient(settings.LlmModel, settings.OpenAiApiKey, TimeSpan.FromSeconds(settings.ChatTimeoutSeconds), maxRetries: 1,
            services.GetRequiredService<ILogger<LlmJsonClient>>()),
        PromptsDir(services, settings),
        settings.LlmChatReasoningEffort);
});
builder.Services.AddSingleton(services =>
{
    var settings = services.GetRequiredService<IOptions<FlatplannerOptions>>().Value;
    return new ListingJudge(
        new LlmJsonClient(
            string.IsNullOrWhiteSpace(settings.LlmJudgeModel) ? settings.LlmModel : settings.LlmJudgeModel,
            settings.OpenAiApiKey, ListingJudge.Timeout, maxRetries: 1, services.GetRequiredService<ILogger<LlmJsonClient>>()),
        PromptsDir(services, settings),
        settings.LlmChatReasoningEffort,
        services.GetRequiredService<ILogger<ListingJudge>>());
});
builder.Services.AddSingleton<ConcurrentDictionary<string, AskEvaluation>>();
builder.Services.AddScoped<AskService>();

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
app.MapAsk();
app.Map("/api/{**rest}", () => Results.Problem(statusCode: StatusCodes.Status404NotFound));
app.MapFallbackToFile("index.html");

app.Run();

static string PromptsDir(IServiceProvider services, FlatplannerOptions settings) =>
    Path.Combine(services.GetRequiredService<IHostEnvironment>().ContentRootPath, settings.PromptsDir);
