// ==========================================================================
// ICheckingAccountGrain.cs - Grain Interface Definition
// ==========================================================================
// This file defines the PUBLIC CONTRACT (interface) for a Checking Account grain.
//
// WHAT IS A GRAIN INTERFACE?
// In Orleans, grains communicate through interfaces - just like how in C#,
// classes implement interfaces. The grain interface defines WHAT operations
// a grain can perform, without specifying HOW they're implemented.
//
// KEY CONCEPT: IGrainWithGuidKey
// This interface extends IGrainWithGuidKey, meaning each instance of this grain
// is uniquely identified by a GUID. When you call clusterClient.GetGrain<ICheckingAccountGrain>(someGuid),
// Orleans either creates a new grain with that GUID or finds an existing one.
// Think of the GUID as the grain's "primary key" or "address".
//
// In a real bank, each checking account would have its own GUID, and therefore
// its own grain instance - providing natural isolation between accounts.
// ==========================================================================

using Orleans.Concurrency;

namespace Orleans.Grains.Abstractions
{
    /// <summary>
    /// Defines the operations available on a Checking Account grain.
    /// 
    /// LEARNING NOTE - GRAIN INTERFACE RULES:
    /// 1. All methods must return Task or Task<T> (or ValueTask/ValueTask<T>)
    /// 2. Methods cannot have ref/out parameters
    /// 3. The interface must inherit from IGrainWithGuidKey, IGrainWithIntegerKey,
    ///    IGrainWithStringKey, or one of the other Orleans grain key types
    /// 4. Return types must be serializable by Orleans
    /// </summary>
    public interface ICheckingAccountGrain : IGrainWithGuidKey
    {
        /// <summary>
        /// Initializes the checking account with an opening balance.
        /// Uses TransactionOption.Create to start a NEW transaction.
        /// 
        /// TRANSACTION BASICS:
        /// Transactions in Orleans ensure ACID properties across grain state changes.
        /// TransactionOption.Create means: "start a brand new transaction."
        /// If this method is called inside another transaction, it will fail -
        /// you cannot have a transaction within a transaction with Create.
        /// </summary>
        [Transaction(TransactionOption.Create)]
        Task Initialise(decimal openingBalance);

        /// <summary>
        /// Returns the current balance of the checking account.
        /// Also wrapped in TransactionOption.Create because it reads from
        /// transactional state (ITransactionalState), which can only be
        /// accessed within a transaction context.
        /// </summary>
        [Transaction(TransactionOption.Create)]
        Task<decimal> GetBalance();

        /// <summary>
        /// Deducts the specified amount from the account balance.
        /// Uses TransactionOption.CreateOrJoin, which means:
        /// - If no transaction exists, create a new one
        /// - If a transaction already exists (e.g., from the caller), join it
        /// 
        /// This is crucial! When Transfer() calls both Debit() on one account
        /// and Credit() on another, both operations need to be in the SAME
        /// transaction. CreateOrJoin enables this nesting behavior.
        /// </summary>
        [Transaction(TransactionOption.CreateOrJoin)]
        Task Debit(decimal amount);

        /// <summary>
        /// Adds the specified amount to the account balance.
        /// Also uses CreateOrJoin so it can participate in a larger transaction
        /// (like a transfer between accounts).
        /// </summary>
        [Transaction(TransactionOption.CreateOrJoin)]
        Task Credit(decimal amount);

        /// <summary>
        /// Adds a recurring payment (like a monthly subscription) to this account.
        /// This does NOT use transactions because it's just setting up configuration -
        /// it doesn't modify the actual balance.
        /// 
        /// Internally, this uses Orleans Reminders to trigger automatic debits
        /// at the specified interval. See the implementation for details.
        /// </summary>
        Task AddReccuringPayment(Guid id, decimal amount, int reccursEveryMinutes);

        /// <summary>
        /// Demonstrates ORLEANS CANCELLATION TOKENS.
        /// Similar to CancellationToken in standard .NET, but works across
        /// the network - the client can cancel a long-running grain operation.
        /// 
        /// This grain method simulates a long-running task (like a batch process)
        /// that can be gracefully stopped.
        /// </summary>
        Task CancelableWork(GrainCancellationToken grainCancellationToken, long workDurationSeconds);

        /// <summary>
        /// Demonstrates FIRE-AND-FORGET (OneWay) calls.
        /// The [OneWay] attribute means:
        /// - The caller does NOT wait for the method to complete
        /// - The caller gets back a completed Task immediately
        /// - The grain executes the work asynchronously
        /// - The caller cannot know if the work succeeded or failed
        /// 
        /// Use cases: logging, notifications, background processing
        /// where you don't need a response.
        /// 
        /// IMPORTANT: The method is intentionally designed to throw
        /// NotSupportedException - this demonstrates that the caller
        /// will NEVER know about this failure!
        /// </summary>
        [OneWay]
        Task FireAndForgetWork();
    }
}
