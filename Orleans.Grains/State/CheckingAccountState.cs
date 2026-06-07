// ==========================================================================
// CheckingAccountState.cs - Persistent (Non-Transactional) State
// ==========================================================================
// This record stores the NON-TRANSACTIONAL state of a checking account.
// 
// WHY IS THIS SEPARATE FROM BalanceState?
// In a real banking system, some data needs ACID transactional guarantees
// (the actual balance) and some doesn't (metadata like account type,
// opening date, recurring payment configuration).
//
// By keeping them separate, we can:
// 1. Use ITransactionalState for balance (ACID guarantees)
// 2. Use IPersistentState for metadata (simpler, faster storage)
// 3. Reduce the scope and cost of transactions
//
// This state is stored in BLOB STORAGE ("blobStorage" provider).
// Unlike Azure Table Storage, blob storage stores the entire object
// as a single document (JSON serialized).
// ==========================================================================

namespace Orleans.Grains.State
{
    /// <summary>
    /// Stores metadata about a checking account.
    /// Does NOT participate in transactions.
    /// 
    /// Storage: Azure Blob Storage (via "blobStorage" provider)
    /// </summary>
    [GenerateSerializer]
    public record CheckingAccountState
    {
        /// <summary>The GUID identifier of this account (matches grain key).</summary>
        [Id(0)]
        public Guid AccountId { get; set; }

        /// <summary>When the account was opened (UTC).</summary>
        [Id(1)]
        public DateTime OpenedAtUtc { get; set; }

        /// <summary>Type of account (e.g., "Default", "Premium", "Student").</summary>
        [Id(2)]
        public string AccountType { get; set; }

        /// <summary>
        /// List of recurring payments configured for this account.
        /// Each payment has its own schedule and amount.
        /// Used by the Reminder system to process automatic debits.
        /// </summary>
        [Id(3)]
        public List<RecurringPayment> RecurringPayments { get; set; } = new List<RecurringPayment>();
    }
}
