// ==========================================================================
// LoggingIncomingGrainCallFilter.cs - SILO-LEVEL INCOMING FILTER
// ==========================================================================
// Orleans allows you to intercept all grain method calls using FILTERS.
// This is similar to ASP.NET Core middleware - you can run code BEFORE
// and AFTER every grain method invocation.
//
// IIncomingGrainCallFilter:
//   Runs on the SILO (server) side, intercepting incoming requests to grains.
//   Registered via: siloBuilder.AddIncomingGrainCallFilter<T>()
//
// USE CASES FOR FILTERS:
// - Logging all grain calls (as demonstrated here)
// - Authentication / Authorization checks
// - Request validation
// - Metrics and monitoring
// - Exception handling
// ==========================================================================

using Microsoft.Extensions.Logging;

namespace Orleans.Grains.Filters
{
    /// <summary>
    /// Logs all incoming grain method calls at the SILO level.
    /// This filter runs for EVERY grain in the cluster.
    /// 
    /// Contrast with AtmGrain.Invoke() which implements IIncomingGrainCallFilter
    /// at the GRAIN level (only for that specific grain type).
    /// 
    /// The filter must call context.Invoke() to pass control to the next
    /// filter (or to the actual grain method if there are no more filters).
    /// Forgetting to call Invoke() means the grain method never executes!
    /// </summary>
    public class LoggingIncomingGrainCallFilter : IIncomingGrainCallFilter
    {
        private readonly ILogger<LoggingIncomingGrainCallFilter> _logger;

        public LoggingIncomingGrainCallFilter(ILogger<LoggingIncomingGrainCallFilter> logger)
        {
            _logger = logger;
        }

        public async Task Invoke(IIncomingGrainCallContext context)
        {
            // Before the grain method executes
            _logger.LogInformation($"Incoming Silo Grain Filter: Recived grain call on '{context.Grain}' to '{context.MethodName}' method");

            // Pass control to the actual grain method (or next filter)
            // If you remove this line, NO grain methods will execute!
            await context.Invoke();

            // After the grain method completes
            // You could add post-processing logic here
        }
    }
}
