// ==========================================================================
// IAtmGrain.cs - ATM Machine Grain Interface
// ==========================================================================
// This defines the contract for an ATM (Automated Teller Machine) grain.
// Each ATM machine in the real world gets its own grain instance.
//
// LEARN BY COMPARISON:
// Notice that IAtmGrain and ICheckingAccountGrain both use IGrainWithGuidKey
// but represent completely different real-world concepts. In Orleans, grains
// are virtual - they don't take up memory until they're used. You can have
// millions of ATM and Account grains without worrying about resource limits.
//
// This grain demonstrates MULTI-GRAIN TRANSACTIONS - the ATM withdraw
// operation needs to update BOTH the ATM's cash balance AND the customer's
// account balance atomically (all or nothing).
// ==========================================================================

namespace Orleans.Grains.Abstractions
{
    /// <summary>
    /// Defines the operations available on an ATM grain.
    /// 
    /// Each ATM is a separate grain, which means:
    /// - ATM 1 and ATM 2 have separate state
    /// - They can process withdrawals in parallel without conflicts
    /// - Each ATM tracks its own cash inventory
    /// </summary>
    public interface IAtmGrain : IGrainWithGuidKey
    {
        /// <summary>
        /// Initializes the ATM with a starting cash balance.
        /// Creates a new transaction (TransactionOption.Create).
        /// </summary>
        [Transaction(TransactionOption.Create)]
        public Task Initialise(decimal openingBalance);

        /// <summary>
        /// Processes a withdrawal from this ATM.
        /// Uses CreateOrJoin because this is typically called as PART of
        /// a larger transaction that also debits the checking account.
        /// 
        /// The ATM reduces its cash inventory AND the customer's account
        /// is debited - all in one atomic operation.
        /// </summary>
        [Transaction(TransactionOption.CreateOrJoin)]
        public Task Withdraw(Guid checkingAccountId, decimal amount);

        /// <summary>
        /// Gets the current cash balance remaining in this ATM.
        /// </summary>
        [Transaction(TransactionOption.Create)]
        Task<decimal> GetBalance();
    }
}
