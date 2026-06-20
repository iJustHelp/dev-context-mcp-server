using DevContextMcp.Server.Configuration;
using DevContextMcp.Server.Core.Infrastructure;
using DevContextMcp.Server.Core.Models.Analytics;
using DevContextMcp.Server.Core.Models.Context;
using DevContextMcp.Server.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace DevContextMcp.Server.Context;

internal static class ContextEndpoints
{
    public static void MapContextEndpoints(this WebApplication app)
    {
        app.MapGet("/api/context", GetContextAsync)
            .WithName("GetIndexedContext")
            .WithTags("Context")
            .Produces<IndexedContextResponse>()
            .Produces<ApiError>(StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<IResult> GetContextAsync(
        INuGetReadStore store,
        IOptions<DevContextMcpOptions> options,
        CancellationToken cancellationToken)
    {
        try
        {
            var context = await store.GetIndexedContextAsync(
                Path.GetFullPath(options.Value.DatabasePath, AppContext.BaseDirectory),
                cancellationToken);
            return Results.Json(context);
        }
        catch (IndexUnavailableException exception)
        {
            return Results.Json(
                new ApiError(exception.Message),
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
