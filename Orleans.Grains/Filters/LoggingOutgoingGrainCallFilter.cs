// ==========================================================================
// LoggingOutgoingGrainCallFilter.cs - CLIENT-LEVEL OUTGOING FILTER
// ==========================================================================
// IOutgoingGrainCallFilter:
//   Runs on the CLIENT side, intercepting outgoing requests TO grains.
//   Registered via: client.AddOutgoingGrainCallFilter<T>()
//
// INCOMING vs OUTGOING FILTERS:
// - Incoming filter (silo side): Runs when a request ARRIVES at a grain
// - Outgoing filter (client side): Runs when a request LEAVES the client
//
// You can have BOTH at the same time for different purposes:
// - Client-side: Logging, client metrics, request pre-processing
// - Silo-side: Authentication, server metrics, request validation
// ==========================================================================

using Microsoft.Extensions.Logging;

namespace Orleans.Grains.Filters
{
    /// <summary>
    /// Logs all outgoing grain method calls from the CLIENT side.
    /// This filter runs on every request made from the Orleans.Client project
    /// to any grain in the cluster.
    /// 
    /// Registered in Orleans.Client/Program.cs via:
    ///   client.AddOutgoingGrainCallFilter<LoggingOutgoingGrainCallFilter>()
    /// </summary>
    public class LoggingOutgoingGrainCallFilter : IOutgoingGrainCallFilter
    {
        private readonly ILogger<LoggingOutgoingGrainCallFilter> _logger;

        public LoggingOutgoingGrainCallFilter(ILogger<LoggingOutgoingGrainCallFilter> logger)
        {
            _logger = logger;
        }

        public async Task Invoke(IOutgoingGrainCallContext context)
        {
            // Before the request is sent to the grain
            _logger.LogInformation($"Outgoing Silo Grain Filter: Recived grain call on '{context.Grain}' to '{context.MethodName}' method");

            // Send the request to the grain
            await context.Invoke();

            // After the response comes back
            // You could log response time, success/failure, etc.
        }
    }
}
