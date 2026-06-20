using DevContextMcp.Server;
using DevContextMcp.Server.api.Analytics;
using DevContextMcp.Server.api.Context;
using DevContextMcp.Server.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using ModelContextProtocol.Server;
using Serilog;

await RunHttpAsync(args);

static async Task RunHttpAsync(string[] args)
{
    var builder = WebApplication.CreateBuilder(
        new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory
        });

    ConfigureLogging(builder.Services, builder.Configuration);
    builder.Services.AddDevContextMcpCore(builder.Configuration);
    builder.Services.AddOpenApi(options =>
    {
        // .NET emits 64-bit numbers (long/double) as type ["integer"|"number","string"]
        // so out-of-range values can round-trip as JSON strings. This server always writes
        // plain JSON numbers, so collapse the union back to the numeric type — generated
        // clients then see `number` instead of `number | string`.
        options.AddSchemaTransformer((schema, context, cancellationToken) =>
        {
            if (schema.Type is { } type
                && type.HasFlag(JsonSchemaType.String)
                && (type.HasFlag(JsonSchemaType.Integer) || type.HasFlag(JsonSchemaType.Number)))
            {
                schema.Type = type & ~JsonSchemaType.String;
                schema.Pattern = null;
            }

            return Task.CompletedTask;
        });
    });
    builder.Services.AddMcpServer()
        .WithHttpTransport(options => options.Stateless = true)
        .WithDevContextMcpTools();

    var app = builder.Build();
    app.MapOpenApi();
    var options = app.Services
        .GetRequiredService<IOptions<DevContextMcpOptions>>()
        .Value;
    var mcpUri = new Uri(options.McpUrl, UriKind.Absolute);
    app.MapMcp(mcpUri.AbsolutePath);
    if (options.Analytics.Enabled)
    {
        app.MapAnalyticsEndpoints();
    }
    app.MapContextEndpoints();

    await app.RunAsync(mcpUri.GetLeftPart(UriPartial.Authority));
}

static void ConfigureLogging(
    IServiceCollection services,
    IConfiguration configuration)
{
    ApplyServerLoggingRequirements(configuration);
    ResolveRelativeFileSinkPaths(configuration);
    services.AddSerilog((registeredServices, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(configuration)
        .ReadFrom.Services(registeredServices));
}

static void ApplyServerLoggingRequirements(IConfiguration configuration)
{
    configuration[
        "Serilog:MinimumLevel:Override:Microsoft.Hosting.Lifetime"] = "Information";

    foreach (var sink in configuration.GetSection("Serilog:WriteTo").GetChildren())
    {
        if (string.Equals(sink["Name"], "Console", StringComparison.OrdinalIgnoreCase))
        {
            sink["Args:standardErrorFromLevel"] = "Verbose";
        }
    }
}

static void ResolveRelativeFileSinkPaths(IConfiguration configuration)
{
    foreach (var sink in configuration.GetSection("Serilog:WriteTo").GetChildren())
    {
        if (!string.Equals(sink["Name"], "File", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        var path = sink["Args:path"];
        if (!string.IsNullOrWhiteSpace(path) && !Path.IsPathRooted(path))
        {
            sink["Args:path"] = Path.GetFullPath(path, AppContext.BaseDirectory);
        }
    }
}

/// <summary>
/// Marker partial type that exposes the server entry point assembly to integration tests.
/// </summary>
public partial class Program;
