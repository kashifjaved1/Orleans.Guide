// ==========================================================================
// StatlessTransferProcessingGrain.cs - Stateless Worker Orchestrator
// ==========================================================================
// This grain demonstrates one of the most important Orleans patterns:
// the ORCHESTRATOR pattern using [StatelessWorker].
//
// WHAT IS [StatelessWorker]?
// ---------------------------------------------------------------
// Normally, each grain identity (key) can have only ONE activation
// (instance) in the entire Orleans cluster. This ensures single-threaded
// execution per grain.
//
// [StatelessWorker] CHANGES THIS RULE:
// - Multiple activations of this grain can run SIMULTANEOUSLY
// - Each activation can be on ANY silo in the cluster
// - The grain does NOT maintain state (or the state it keeps is disposable)
// - Orleans automatically load-balances requests across activations
//
// WHEN TO USE STATELESS WORKER:
// - Request routing / orchestration
// - Stateless computation
// - Work that can be done in parallel
// - Tasks where you don't need single-instance guarantees
//
// THE ORCHESTRATOR PATTERN:
// ---------------------------------------------------------------
// The orchestrator grain coordinates work between multiple other grains.
// It's responsible for:
// 1. Getting references to the relevant grains
// 2. Ensuring the correct order of operations
// 3. Handling failures (e.g., compensating transactions)
// 4. Wrapping everything in a transaction
//
// IMPORTANT: StatelessWorker grains CAN have state (like our TransferCount),
// but the state is PER-ACTIVATION, not shared across activations.
// Each activation has its own copy!
// ==========================================================================

using Orleans.Concurrency;
using Orleans.Grains.Abstractions;
using Orleans.Grains.State;
using Orleans.Runtime;

namespace Orleans.Grains.Grains
{
    /// <summary>
    /// [StatelessWorker] - Orleans will create MULTIPLE instances of this
    /// grain across the cluster. This makes transfers highly scalable -
    /// multiple transfers can be processed in parallel.
    /// 
    /// However, because there are multiple activations, the TransferCount
    /// will NOT be accurate if many transfers happen simultaneously.
    /// Each activation increments its own local count.
    /// In a real app, you'd use a separate grain or database for accurate counting.
    /// </summary>
    [StatelessWorker]
    public class StatlessTransferProcessingGrain : Grain, IStatlessTransferProcessingGrain
    {
        /// <summary>
        /// ITransactionClient - used to create transactions from within a grain.
        /// Needed here because this grain orchestrates the debit + credit
        /// as a single atomic transaction.
        /// </summary>
        private readonly ITransactionClient _transactionClient;

        /// <summary>
        /// Persistent state to track transfer count.
        /// IMPORTANT LIMITATION: Since this is a [StatelessWorker], multiple
        /// activations exist. Each activation has its own copy of this state.
        /// The TransferCount is approximate, not exact!
        /// </summary>
        private readonly IPersistentState<TransferState> _transferState;

        public StatlessTransferProcessingGrain(
           ITransactionClient transactionClient,
           [PersistentState("transfer", "tableStorage")] IPersistentState<TransferState> transferState)
        {
            _transactionClient = transactionClient;
            _transferState = transferState;
        }

        /// <summary>
        /// Processes a transfer between two accounts.
        /// 
        /// ORCHESTRATION FLOW:
        /// 1. Get references to the source and destination account grains
        /// 2. Start a transaction
        /// 3. Credit the destination account
        /// 4. Debit the source account
        /// 5. If either step fails, the entire transaction rolls back
        /// 6. Increment the transfer counter
        /// 
        /// WHY CREDIT FIRST THEN DEBIT?
        /// This reduces the chance of the account appearing to lose money
        /// temporarily. In a real system, you'd credit first to ensure
        /// the destination exists and is valid, then debit the source.
        /// If the debit fails, the credit is rolled back by the transaction.
        /// </summary>
        public async Task ProcessTransfer(Guid fromAccountId, Guid toAccountId, decimal amount)
        {
            // Get references to both account grains using GrainFactory
            // (GrainFactory is inherited from the Grain base class)
            var fromAccountGrain = GrainFactory.GetGrain<ICheckingAccountGrain>(fromAccountId);
            var toAccountGrain = GrainFactory.GetGrain<ICheckingAccountGrain>(toAccountId);

            // Execute both operations within a SINGLE transaction
            // This provides: Atomicity, Consistency, Isolation, Durability
            await _transactionClient.RunTransaction(TransactionOption.Create, async () =>
            {
                await toAccountGrain.Credit(amount);
                await fromAccountGrain.Debit(amount);
            });

            // Update the transfer counter (approximate for StatelessWorker)
            _transferState.State.TransferCount += 1;
            await _transferState.WriteStateAsync();
        }
    }
}
