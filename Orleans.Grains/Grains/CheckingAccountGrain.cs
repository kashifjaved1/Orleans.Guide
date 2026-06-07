// ==========================================================================
// CheckingAccountGrain.cs - The Core Banking Grain
// ==========================================================================
// This is the MOST IMPORTANT grain in the project — it demonstrates the
// most Orleans concepts. Read this file carefully!
//
// ORLEANS BASICS - WHAT IS A GRAIN?
// A grain is the fundamental unit of computation in Orleans. Think of it as
// a virtual actor - an object that:
//   1. Has a unique identity (its Guid key)
//   2. Has private state (data)
//   3. Processes requests ONE AT A TIME (single-threaded by default)
//   4. Can persist its state to storage
//   5. Can communicate with other grains
//   6. Is "virtual" - always exists conceptually, even if not in memory
//
// REAL-WORLD ANALOGY:
// Each CheckingAccountGrain = One bank checking account.
// Just like in a real bank, each account has its own balance, its own
// transactions, and operations on it are serialized (one teller at a time).
// ==========================================================================

using Orleans.Concurrency;
using Orleans.Grains.Abstractions;
using Orleans.Grains.Events;
using Orleans.Grains.State;
using Orleans.Runtime;
using Orleans.Transactions.Abstractions;

namespace Orleans.Grains.Grains
{
    /// <summary>
    /// [Reentrant] - ADVANCED CONCEPT
    /// By default, Orleans processes ONE message at a time per grain.
    /// This is called "turn-based" execution and prevents race conditions.
    /// 
    /// [Reentrant] ALLOWS INTERLEAVING:
    /// With this attribute, if grain A calls grain B, and grain B calls back
    /// to grain A, Orleans will allow the callback to be processed even
    /// though grain A is still busy with the original call.
    /// 
    /// Without [Reentrant], this would cause a DEADLOCK!
    /// Use [Reentrant] carefully - only when you know it's safe.
    /// 
    /// IRemindable - REMINDERS:
    /// This interface allows the grain to receive timer-based callbacks
    /// (reminders). Even if the grain is deactivated (removed from memory),
    /// the reminder will persist and reactivate the grain when it fires.
    /// This is perfect for recurring payments!
    /// </summary>
    [Reentrant]
    public class CheckingAccountGrain : Grain, ICheckingAccountGrain, IRemindable
    {
        // ======================================================================
        // DEPENDENCY INJECTION IN GRAINS
        // ======================================================================
        // Orleans supports constructor injection just like ASP.NET Core.
        // Additionally, Orleans provides special attributes for injecting
        // Orleans-specific services:
        //
        // [TransactionalState("name")] - Injects transactional state storage
        // [PersistentState("name", "storageProvider")] - Injects non-transactional storage
        //
        // These are resolved by the Orleans runtime, not a standard DI container.
        // ======================================================================

        /// <summary>
        /// ITransactionClient - Allows this grain to START transactions
        /// programmatically. Normally, transactions are started by the client.
        /// But grains can also start transactions if needed (e.g., from within
        /// a reminder callback which has no transaction context).
        /// </summary>
        private readonly ITransactionClient _transactionClient;

        /// <summary>
        /// ITransactionalState<BalanceState> - TRANSACTIONAL STORAGE
        /// This stores the account balance and participates in ACID transactions.
        /// When you call Debit() and Credit() inside a transaction, the balance
        /// changes are committed atomically.
        /// 
        /// If something fails mid-transaction, the balance is automatically
        /// rolled back to its previous value!
        /// 
        /// "balance" is the state name - used to identify this state in storage.
        /// </summary>
        private readonly ITransactionalState<BalanceState> _balanceTransactionalState;

        /// <summary>
        /// IPersistentState<CheckingAccountState> - PERSISTENT (non-transactional) STORAGE
        /// This stores account metadata (opening date, account type, recurring payments).
        /// Unlike ITransactionalState, this does NOT participate in transactions.
        /// 
        /// "blobStorage" refers to the storage provider configured in Silo/Program.cs.
        /// This data is stored in Azure Blob Storage.
        /// 
        /// WHY TWO DIFFERENT STORAGE MECHANISMS?
        /// - Balance: Must be transactional (ACID) - use ITransactionalState
        /// - Account info/Metadata: Not transaction-critical - use IPersistentState
        /// 
        /// This separation is a best practice - only put truly critical
        /// financial data in transactions to keep them fast.
        /// </summary>
        private readonly IPersistentState<CheckingAccountState> _checkingAccountState;

        public CheckingAccountGrain(
            ITransactionClient transactionClient,
            [TransactionalState("balance")] ITransactionalState<BalanceState> balanceTransactionalState,
            [PersistentState("checkingAccount", "blobStorage")] IPersistentState<CheckingAccountState> checkingAccountState)
        {
            _transactionClient = transactionClient;
            _balanceTransactionalState = balanceTransactionalState;
            _checkingAccountState = checkingAccountState;
        }

        // ======================================================================
        // METHOD 1: AddReccuringPayment - Using ORLEANS REMINDERS
        // ======================================================================
        // 
        // WHAT ARE REMINDERS?
        // Reminders are like cron jobs or scheduled tasks for grains.
        // They are PERSISTENT - even if the silo restarts, reminders survive.
        // They are RELIABLE - Orleans guarantees that reminders will fire.
        //
        // HOW IT WORKS:
        // 1. Save the recurring payment details to persistent storage
        // 2. Register a reminder with Orleans
        // 3. When the reminder fires, the grain's ReceiveReminder() is called
        // 4. The grain processes the recurring debit
        //
        // REMINDER PARAMETERS:
        // - Reminder name: A unique string to identify this reminder
        // - Due time: When to fire for the first time
        // - Period: How often to repeat
        // ======================================================================
        public async Task AddReccuringPayment(Guid id, decimal amount, int reccursEveryMinutes)
        {
            // Step 1: Save the payment configuration to persistent storage
            _checkingAccountState.State.RecurringPayments.Add(new RecurringPayment
            {
                PaymentId = id,
                PaymentAmount = amount,
                OccursEveryMinutes = reccursEveryMinutes
            });

            // State is in-memory until we call WriteStateAsync()
            await _checkingAccountState.WriteStateAsync();

            // Step 2: Register a reminder that will fire periodically
            // The reminder name includes the payment ID so we can identify
            // which recurring payment triggered this reminder later.
            await this.RegisterOrUpdateReminder(
                $"RecurringPayment:::{id}",                              // Unique reminder name
                TimeSpan.FromMinutes(reccursEveryMinutes),               // First fire time
                TimeSpan.FromMinutes(reccursEveryMinutes)                // Repeat interval
            );
        }

        // ======================================================================
        // METHOD 2: ReceiveReminder - HANDLING REMINDER FIRINGS
        // ======================================================================
        // This method is called when a reminder fires. It's part of the
        // IRemindable interface.
        //
        // CRITICAL DETAIL: This method runs OUTSIDE any transaction context!
        // The reminder system just calls this method - it doesn't create a
        // transaction. That's why we use _transactionClient.RunTransaction()
        // to create a transaction manually before calling Debit().
        // ======================================================================
        public async Task ReceiveReminder(string reminderName, TickStatus status)
        {
            // Check if this is a recurring payment reminder
            if (reminderName.StartsWith("RecurringPayment"))
            {
                // Extract the payment ID from the reminder name
                var reccuringPaymentId = Guid.Parse(reminderName.Split(":::").Last());

                // Find the payment configuration from our stored state
                var reccuringPayment = _checkingAccountState.State.RecurringPayments
                    .Single(x => x.PaymentId == reccuringPaymentId);

                // Start a transaction manually (reminder callbacks don't have one)
                await _transactionClient.RunTransaction(TransactionOption.Create, async () =>
                {
                    // Debit the account for the recurring payment amount
                    await Debit(reccuringPayment.PaymentAmount);
                });
            }
        }

        // ======================================================================
        // METHOD 3: Credit - Adding Money + ORLEANS STREAMS
        // ======================================================================
        //
        // WHAT ARE ORLEANS STREAMS?
        // Streams are Orleans' answer to event streaming / pub-sub messaging.
        // They allow grains to PUBLISH events and other grains to SUBSCRIBE.
        //
        // KEY STREAM CONCEPTS:
        // - StreamProvider: Configured in Silo/Program.cs ("StreamProvider")
        // - StreamId: A namespace + key pair that identifies the stream
        //   Here we use namespace="BalanceStream" and key=the account's GUID
        // - Each account has its OWN stream - only interested subscribers listen
        //
        // FLOW:
        // 1. Balance changes (debit or credit)
        // 2. Publish BalanceChangeEvent to the stream
        // 3. CustomerGrain (subscribed to this stream) receives the update
        // 4. CustomerGrain updates its cached view of the customer's net worth
        //
        // This is an EVENT-DRIVEN architecture within the Orleans cluster!
        // ======================================================================
        public async Task Credit(decimal amount)
        {
            // Step 1: Update the balance using transactional state
            await _balanceTransactionalState.PerformUpdate(state =>
            {
                var currentBalance = state.Balance;
                var newBalance = currentBalance + amount;
                state.Balance = newBalance;
            });

            // Step 2: Publish a balance change event to the stream
            var streamProvider = this.GetStreamProvider("StreamProvider");
            var streamId = StreamId.Create("BalanceStream", this.GetGrainId().GetGuidKey());
            var stream = streamProvider.GetStream<BalanceChangeEvent>(streamId);

            await stream.OnNextAsync(new BalanceChangeEvent()
            {
                CheckingAccountId = this.GetGrainId().GetGuidKey(),
                Balance = await GetBalance()
            });
        }

        // ======================================================================
        // METHOD 4: Debit - Withdrawing Money
        // ======================================================================
        // Same pattern as Credit, but subtracts instead of adds.
        // In a real app, you'd add overdraft protection checks here!
        // ======================================================================
        public async Task Debit(decimal amount)
        {
            await _balanceTransactionalState.PerformUpdate(state =>
            {
                var currentBalance = state.Balance;
                var newBalance = currentBalance - amount;
                state.Balance = newBalance;
            });

            // Publish the updated balance via streams
            var streamProvider = this.GetStreamProvider("StreamProvider");
            var streamId = StreamId.Create("BalanceStream", this.GetGrainId().GetGuidKey());
            var stream = streamProvider.GetStream<BalanceChangeEvent>(streamId);

            await stream.OnNextAsync(new BalanceChangeEvent()
            {
                CheckingAccountId = this.GetGrainId().GetGuidKey(),
                Balance = await GetBalance()
            });
        }

        // ======================================================================
        // METHOD 5: GetBalance - Reading Transactional State
        // ======================================================================
        // PerformRead is the read-only version of PerformUpdate.
        // It reads the value without making changes.
        // With transactions, reads are also isolated - you see a consistent snapshot.
        // ======================================================================
        public async Task<decimal> GetBalance()
        {
            return await _balanceTransactionalState.PerformRead(state => state.Balance);
        }

        // ======================================================================
        // METHOD 6: Initialise - Setting Up a New Account
        // ======================================================================
        // Demononstrates writing to BOTH transactional and persistent state.
        // The opening balance goes to transactional state (needs ACID guarantees).
        // The metadata (opening date, account type) goes to persistent state.
        // ======================================================================
        public async Task Initialise(decimal openingBalance)
        {
            // Set account metadata in persistent (non-transactional) state
            _checkingAccountState.State.OpenedAtUtc = DateTime.UtcNow;
            _checkingAccountState.State.AccountType = "Default";
            _checkingAccountState.State.AccountId = this.GetGrainId().GetGuidKey();

            // Set opening balance in transactional state
            await _balanceTransactionalState.PerformUpdate(state =>
            { 
                state.Balance = openingBalance;
            });

            // Persist the metadata to blob storage
            await _checkingAccountState.WriteStateAsync();
        }

        // ======================================================================
        // METHOD 7: FireAndForgetWork - ONE-WAY CALL DEMONSTRATION
        // ======================================================================
        // This method has the [OneWay] attribute on the interface.
        // 
        // WHAT THIS MEANS:
        // The client sends this request and IMMEDIATELY gets back a completed Task.
        // The grain will execute the work asynchronously.
        // The client NEVER knows the result - even if the grain throws!
        //
        // WHY IS THIS USEFUL?
        // Fire-and-forget is great for:
        // - Logging
        // - Notifications
        // - Analytics tracking
        // - Any work where you don't need a response
        //
        // THE TRICK:
        // This method intentionally throws NotSupportedException to prove
        // the caller never sees the error. Run this endpoint and check the
        // SILO's console output to see the exception!
        // ======================================================================
        public async Task FireAndForgetWork()
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
            throw new NotSupportedException("This work cannot be done");
        }

        // ======================================================================
        // METHOD 8: CancelableWork - GRAIN CANCELLATION TOKENS
        // ======================================================================
        //
        // WHAT ARE GRAIN CANCELLATION TOKENS?
        // Just like CancellationToken in .NET, but works ACROSS THE NETWORK.
        // The client creates a GrainCancellationTokenSource and passes the
        // token to the grain method. If the client cancels, the grain gets
        // notified even though it's running on a different machine!
        //
        // USE CASE:
        // Long-running batch jobs, large file processing, or any operation
        // that might need to be cancelled by the user.
        // ======================================================================
        public async Task CancelableWork(GrainCancellationToken grainCancellationToken, long workDurationSeconds)
        {
            try
            {
                // Simulate long-running work with a delay
                await Task.Delay(TimeSpan.FromSeconds(workDurationSeconds), grainCancellationToken.CancellationToken);
            }
            catch (TaskCanceledException _)
            {
                // Gracefully handle cancellation
                // In a real app, you'd clean up resources here
                return;
            }
        }
    }
}
