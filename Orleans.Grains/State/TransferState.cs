// ==========================================================================
// TransferState.cs - Transfer Counter (StatelessWorker Caveat)
// ==========================================================================
// Simple counter to track how many transfers have been processed.
// 
// IMPORTANT LIMITATION:
// Since this is used by StatlessTransferProcessingGrain (a [StatelessWorker]),
// multiple activations exist. Each activation has its own TransferState.
// The count is APPROXIMATE, not exact.
//
// This demonstrates an important architectural consideration:
// If you need ACCURATE global counters, don't use [StatelessWorker] state.
// Use a dedicated grain, a database, or a distributed counter instead.
// ==========================================================================

namespace Orleans.Grains.State
{
    /// <summary>
    /// Tracks the number of transfers processed (approximate count).
    /// 
    /// Storage: Azure Table Storage (via "tableStorage" provider)
    /// </summary>
    [GenerateSerializer]
    public record TransferState
    {
        /// <summary>Number of transfers processed by this grain activation.</summary>
        [Id(0)]
        public int TransferCount { get; set; }
    }
}
