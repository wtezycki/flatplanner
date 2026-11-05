using Flatplanner.Core.Data;
using Flatplanner.Core.Options;
using Flatplanner.Ingest.Normalization;
using Flatplanner.Ingest.Reporting;
using Flatplanner.Ingest.Scraping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);

builder.Services.AddOptions<FlatplannerOptions>()
    .BindConfiguration("")
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddDbContext<FlatplannerDbContext>((services, db) =>
{
    var settings = services.GetRequiredService<IOptions<FlatplannerOptions>>().Value;
    db.UseMySql(settings.ConnectionString, DbConstants.ServerVersion, mysql => mysql.EnableRetryOnFailure(5));
});
builder.Services.AddTransient<OtodomScraper>();
builder.Services.AddTransient<NormalizeCommand>();
builder.Services.AddTransient<ReportCommand>();

using var host = builder.Build();
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var command = args.FirstOrDefault();
string[] commands = ["scrape", "normalize", "report", "all"];

if (command is null || !commands.Contains(command))
{
    logger.LogError("Usage: Flatplanner.Ingest <{Commands}>", string.Join('|', commands));
    return 2;
}

await using var scope = host.Services.CreateAsyncScope();
var services = scope.ServiceProvider;
await services.GetRequiredService<FlatplannerDbContext>().Database.MigrateAsync();

if (command is "scrape" or "all")
{
    using var scraper = services.GetRequiredService<OtodomScraper>();
    (await scraper.RunAsync(CancellationToken.None)).Write(Console.Out);
}

if (command is "normalize" or "all")
{
    await services.GetRequiredService<NormalizeCommand>().RunAsync(CancellationToken.None);
}

if (command is "report" or "all")
{
    await services.GetRequiredService<ReportCommand>().RunAsync(CancellationToken.None);
}

return 0;
