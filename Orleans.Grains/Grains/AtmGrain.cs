// ==========================================================================
// AtmGrain.cs - ATM Machine Grain Implementation
// ==========================================================================
// This grain represents a physical ATM machine. Each ATM has its own
// cash inventory and can dispense money to customers.
//
// KEY LEARNING - GRAIN-TO-GRAIN COMMUNICATION:
// Inside a grain method, you can get references to OTHER grains using
// GrainFactory.GetGrain<T>(key). This allows grains to collaborate.
//
// The AtmGrain demonstrates:
// 1. Transactional state for ATM cash balance
// 2. Getting references to other grains (ICheckingAccountGrain)
// 3. Implementing IIncomingGrainCallFilter AT THE GRAIN LEVEL
//    (as opposed to the silo-wide filter in Filters/ folder)
// ==========================================================================

using Microsoft.Extensions.Logging;
using Orleans.Concurrency;
using Orleans.Grains.Abstractions;
using Orleans.Grains.State;
using Orleans.Runtime;
using Orleans.Transactions.Abstractions;

namespace Orleans.Grains.Grains
{
    /// <summary>
    /// [Reentrant] - Allows safe callback interleaving.
    /// Since the ATM grain receives calls from the client and also calls
    /// back to checking account grains, [Reentrant] prevents deadlocks
    /// that could occur if the checking account grain tries to call back
    /// to this ATM grain.
    /// </summary>
    [Reentrant]
    public class AtmGrain : Grain, IAtmGrain, IIncomingGrainCallFilter
    {
        /// <summary>
        /// ITransactionalState<AtmState> - Stores the ATM's cash inventory
        /// using Orleans' transactional storage. This ensures that when
        /// money is dispensed, the ATM's balance is updated atomically
        /// with the customer's account being debited.
        /// </summary>
        private readonly ITransactionalState<AtmState> _atmTransactionalState;

        /// <summary>
        /// Standard .NET ILogger for logging grain activity.
        /// Orleans supports the standard Microsoft.Extensions.Logging abstractions.
        /// </summary>
        private readonly ILogger<AtmGrain> _logger;

        public AtmGrain(
            ILogger<AtmGrain> logger,
            [TransactionalState("atm")] ITransactionalState<AtmState> atmTransactionalState)
        {
            _atmTransactionalState = atmTransactionalState;
            _logger = logger;
        }

        /// <summary>
        /// Initializes the ATM with a starting cash balance.
        /// </summary>
        public async Task Initialise(decimal openingBalance)
        {
            await _atmTransactionalState.PerformUpdate(state =>
            {
                state.Balance = openingBalance;
                state.Id = this.GetGrainId().GetGuidKey();
            });
        }

        /// <summary>
        /// Gets the current cash balance remaining in the ATM.
        /// </summary>
        public async Task<decimal> GetBalance()
        {
            return await _atmTransactionalState.PerformRead((state) => state.Balance);
        }

        /// <summary>
        /// Processes a withdrawal from the ATM.
        /// 
        /// IMPORTANT CONCEPT: Notice that this method updates the ATM's
        /// balance but calls the client code to also debit the checking
        /// account. The TRANSACTION wraps BOTH operations atomically.
        /// 
        /// The actual debit of the checking account happens in the CLIENT
        /// code (Program.cs), not in this grain. This demonstrates that
        /// multi-grain transactions can span grain and client code!
        /// 
        /// INSIDE A TRANSACTION:
        /// The ATM's balance is reduced AND the checking account is debited
        /// - if either fails, BOTH are rolled back.
        /// </summary>
        public async Task Withdraw(Guid checkingAccountId, decimal amount)
        {
            // Get a reference to the customer's checking account grain
            // GrainFactory is inherited from the Grain base class
            var checkingAccountGrain = GrainFactory.GetGrain<ICheckingAccountGrain>(checkingAccountId);

            // Update the ATM's cash balance
            await _atmTransactionalState.PerformUpdate(state =>
            {
                var currentAtmBalance = state.Balance;
                var updatedBalance = currentAtmBalance - amount;
                state.Balance = updatedBalance;
            });
        }

        /// <summary>
        /// GRAIN-LEVEL INCOMING CALL FILTER
        /// 
        /// Unlike the silo-wide filters (LoggingIncomingGrainCallFilter),
        /// this filter is applied ONLY to this specific grain type.
        /// 
        /// It intercepts EVERY method call made to this grain, logs it,
        /// and then passes control to the actual method via context.Invoke().
        /// 
        /// This is useful for:
        /// - Per-grain-type logging or metrics
        /// - Authorization checks specific to this grain
        /// - Input validation
        /// </summary>
        public async Task Invoke(IIncomingGrainCallContext context)
        {
            _logger.LogInformation($"Incoming ATM Grain Filter: Recived grain call on '{context.Grain}' to '{context.MethodName}' method");
            await context.Invoke();
        }
    }
}
