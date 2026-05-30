using System.Reflection;
using Microsoft.Extensions.Options;
using NatsManager.Application.Modules.Permissions.Models;
using NatsManager.Application.Modules.Permissions.Services;
using NatsManager.Infrastructure.Persistence;
using NatsManager.Web.Configuration;
using NatsManager.Web.Endpoints;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "NatsManager")
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}", formatProvider: System.Globalization.CultureInfo.InvariantCulture)
        .WriteTo.File("logs/natsmanager-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            formatProvider: System.Globalization.CultureInfo.InvariantCulture,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {CorrelationId} {SourceContext} {Message:lj}{NewLine}{Exception}"));

builder.AddServiceDefaults();
builder.AddNatsManagerServices();

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();

    var manifestOptions = scope.ServiceProvider.GetRequiredService<IOptions<PermissionManifestOptions>>().Value;
    var manifestRegistry = scope.ServiceProvider.GetRequiredService<IPermissionManifestRegistry>();
    var manifestPublisher = scope.ServiceProvider.GetRequiredService<IPermissionManifestPublisher>();
    manifestPublisher.Publish(manifestRegistry.CreateManifest(
        Program.CreatePermissionManifestApplicationMetadata(manifestOptions)));
}

app.UseNatsManagerPipeline();

app.MapDefaultEndpoints();
app.MapApiEndpoints();

app.MapFallback(async (HttpContext context, IWebHostEnvironment environment) =>
{
    if (context.Request.Path.StartsWithSegments("/api")
        || context.Request.Path.StartsWithSegments("/hubs"))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    var indexFile = environment.WebRootFileProvider.GetFileInfo("index.html");
    if (!indexFile.Exists)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    context.Response.ContentType = "text/html; charset=utf-8";
    context.Response.ContentLength = indexFile.Length;
    await using var stream = indexFile.CreateReadStream();
    await stream.CopyToAsync(context.Response.Body, context.RequestAborted);
});

app.Run();

public partial class Program
{
    protected Program() { }

    internal static ApplicationMetadata CreatePermissionManifestApplicationMetadata(PermissionManifestOptions options)
    {
        var version = !string.IsNullOrWhiteSpace(options.ApplicationVersion)
            ? options.ApplicationVersion.Trim()
            : typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        return new ApplicationMetadata(
            options.ApplicationId.Trim(),
            options.ApplicationName.Trim(),
            string.IsNullOrWhiteSpace(version) ? null : version);
    }
}
