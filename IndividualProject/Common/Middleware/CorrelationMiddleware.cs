using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Week4.Common.Middleware
{
    public class CorrelationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationMiddleware> _logger;

        public const string HeaderName = "X-Correlation-ID";

        public CorrelationMiddleware(RequestDelegate next, ILogger<CorrelationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // If request does not send a correlation ID generate a new one
            if (!context.Request.Headers.TryGetValue(HeaderName, out var correlationId))
            {
                correlationId = Guid.NewGuid().ToString("N");
                context.Request.Headers[HeaderName] = correlationId;
            }

            // always return the correlation ID to the client
            context.Response.Headers[HeaderName] = correlationId;

            // Create logging scope with CorrelationId
            using (_logger.BeginScope(new Dictionary<string, object>
                   {
                       ["CorrelationId"] = correlationId.ToString()
                   }))
            {
                _logger.LogInformation("➡ Starting request with CorrelationId={CorrelationId}", correlationId);

                await _next(context);

                _logger.LogInformation("⬅ Ending request with CorrelationId={CorrelationId}", correlationId);
            }
        }
    }
}