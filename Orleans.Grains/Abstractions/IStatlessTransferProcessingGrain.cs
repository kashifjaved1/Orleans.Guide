// ==========================================================================
// IStatlessTransferProcessingGrain.cs - Stateless Transfer Orchestrator
// ==========================================================================
// This interface defines a STATELESS WORKER grain that orchestrates
// money transfers between two checking accounts.
//
// KEY LEARNING - IGrainWithIntegerKey vs IGrainWithGuidKey:
// - IGrainWithGuidKey: Each grain instance is identified by a Guid
//   (used for entities like accounts, ATMs, customers)
// - IGrainWithIntegerKey: Each grain is identified by a long/Int64
//   (used here because there's effectively just ONE transfer processor)
//
// We use the key "0" (integer) when getting this grain, meaning there's
// essentially a singleton-like transfer processor in the cluster.
// However, because it's [StatelessWorker], Orleans can create MULTIPLE
// instances of it across the cluster for scalability!
// ==========================================================================

namespace Orleans.Grains.Abstractions
{
    /// <summary>
    /// Defines the transfer processing operation.
    /// This grain acts as an ORCHESTRATOR - it coordinates work between
    /// multiple other grains (the source and destination accounts).
    /// 
    /// ORCHESTRATION PATTERN:
    /// Instead of having one account grain call another directly (which 
    /// could cause coupling), we use a dedicated orchestrator grain that:
    /// 1. Gets references to both account grains
    /// 2. Tells one to debit and the other to credit
    /// 3. Ensures both happen atomically within a transaction
    /// </summary>
    public interface IStatlessTransferProcessingGrain : IGrainWithIntegerKey
    {
        /// <summary>
        /// Processes a transfer from one account to another.
        /// Uses the Orchestration pattern to coordinate the debit and credit
        /// within a single transaction, ensuring atomicity.
        /// </summary>
        Task ProcessTransfer(Guid fromAccountId, Guid toAccountId, decimal amount);
    }
}
