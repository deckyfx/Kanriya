using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using Kanriya.Server.Services;
using Serilog.Context;
using System.Text;

namespace Kanriya.Server.Program;

/// <summary>
/// HTTP request interceptor to log GraphQL operations for debugging
/// </summary>
public class GraphQLLoggingInterceptor : DefaultHttpRequestInterceptor
{
    private readonly ILogger<GraphQLLoggingInterceptor> _logger;
    
    public GraphQLLoggingInterceptor(ILogger<GraphQLLoggingInterceptor> logger)
    {
        _logger = logger;
    }

    public override async ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        // Only log in development/staging environments
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        
        if (environment != "Production")
        {
            // Get the raw request body for logging
            context.Request.EnableBuffering();
            
            string requestBody = "";
            if (context.Request.Body.CanSeek)
            {
                context.Request.Body.Position = 0;
                using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true))
                {
                    requestBody = await reader.ReadToEndAsync();
                    context.Request.Body.Position = 0;
                }
            }
            
            var requestId = Guid.NewGuid().ToString();
            
            // Determine operation type from request body
            var operationType = "Query";
            if (requestBody.Contains("mutation", StringComparison.OrdinalIgnoreCase))
                operationType = "Mutation";
            else if (requestBody.Contains("subscription", StringComparison.OrdinalIgnoreCase))
                operationType = "Subscription";
            
            using (LogContext.PushProperty("Tag", "GraphQL"))
            using (LogContext.PushProperty("RequestId", requestId))
            {
                _logger.LogInformation(
                    "[GraphQL] Request | Type: {OperationType} | Path: {Path} | RequestId: {RequestId}",
                    operationType,
                    context.Request.Path,
                    requestId);
                
                // Log the request body in debug mode
                if (!string.IsNullOrEmpty(requestBody))
                {
                    _logger.LogDebug("[GraphQL] Request Body: {RequestBody}", requestBody);
                }
            }
            
            // Store request ID in HttpContext for later use
            context.Items["GraphQLRequestId"] = requestId;
            context.Items["GraphQLStartTime"] = DateTimeOffset.UtcNow;
        }

        await base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
    }
}