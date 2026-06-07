// ==========================================================================
// CustomerGrain.cs - Customer Grain with Stream Subscriptions
// ==========================================================================
// This grain represents a bank customer and demonstrates one of Orleans'
// most powerful features: STREAMS.
//
// ORLEANS STREAMS CONCEPT:
// Streams are a PUB-SUB (Publish-Subscribe) mechanism built into Orleans.
// They allow grains to communicate ASYNCHRONOUSLY through events.
//
// THE PROBLEM STREAMS SOLVE:
// Without streams, if we wanted to know a customer's total net worth across
// all their accounts, we'd have to:
//   1. Call each account grain to get its balance (expensive)
//   2. Keep polling to detect changes (wasteful)
//
// WITH STREAMS:
//   1. Each CheckingAccount grain PUBLISHES balance changes to a stream
//   2. The Customer grain SUBSCRIBES to those streams
//   3. The customer grain maintains a CACHED up-to-date view
//   4. GetNetWorth() is instant - just sum up cached values!
//
// REAL-WORLD ANALOGY:
// Think of streams like a newsletter subscription.
// - CheckingAccount = The publisher (writes the newsletter)
// - Stream provider = The postal service (delivers the newsletters)
// - Customer = The subscriber (receives updates in their mailbox)
// ==========================================================================

using Orleans.Grains.Abstractions;
using Orleans.Grains.Events;
using Orleans.Grains.State;
using Orleans.Runtime;
using Orleans.Streams;

namespace Orleans.Grains.Grains
{
    /// <summary>
    /// IAsyncObserver<BalanceChangeEvent> - STREAM SUBSCRIBER INTERFACE
    /// By implementing this interface, the Customer grain declares:
    /// "I want to receive BalanceChangeEvent notifications."
    /// 
    /// The interface has 3 methods:
    /// - OnNextAsync: Called when a new event is published
    /// - OnCompletedAsync: Called when the stream is complete (no more events)
    /// - OnErrorAsync: Called when an error occurs on the stream
    /// </summary>
    public class CustomerGrain : Grain, ICustomerGrain, IAsyncObserver<BalanceChangeEvent>
    {
        /// <summary>
        /// Persistent state storing the customer's data.
        /// In this case, it stores a dictionary mapping account IDs to their
        /// latest cached balances.
        /// 
        /// "tableStorage" = Azure Table Storage provider (configured in Silo)
        /// </summary>
        private readonly IPersistentState<CustomerState> _customerState;

        public CustomerGrain(
            [PersistentState("customer", "tableStorage")] IPersistentState<CustomerState> customerState)
        {
            _customerState = customerState;
        }

        // ======================================================================
        // OnActivateAsync - GRAIN LIFECYCLE METHOD
        // ======================================================================
        // This is called EVERY TIME the grain is activated (loaded into memory).
        // Orleans grains are "virtual" - they're created on demand and may be
        // deactivated when idle. OnActivateAsync runs after construction.
        //
        // CRITICAL STREAM CONCEPT - RESUBSCRIBING:
        // Stream subscriptions are PERSISTED in the PubSubStore (Azure Table),
        // but the IN-MEMORY subscription callback (this grain) is lost when the
        // grain is deactivated. When the grain reactivates, we need to RESUME
        // our subscription using the persisted handles.
        //
        // GetAllSubscriptionHandles() finds our previous subscriptions.
        // ResumeAsync(this) reconnects us to the stream.
        // ======================================================================
        public override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            // Get the stream provider (configured in Silo/Program.cs)
            var streamProvider = this.GetStreamProvider("StreamProvider");

            // For each checking account that belongs to this customer...
            foreach (var checkingAccountId in _customerState.State.CheckingAccountBalanceById.Keys)
            {
                // Create the stream identifier for this checking account
                var streamId = StreamId.Create("BalanceStream", checkingAccountId);

                // Get the stream object
                var stream = streamProvider.GetStream<BalanceChangeEvent>(streamId);

                // Get ALL previous subscription handles for this stream
                // (these are stored persistently in the PubSubStore)
                var handles = await stream.GetAllSubscriptionHandles();

                // Resume each subscription - this reconnects our grain to the stream
                foreach (var handle in handles)
                {
                    await handle.ResumeAsync(this);
                }
            }
        }

        /// <summary>
        /// Links a checking account to this customer.
        /// After this call, the customer grain will subscribe to balance
        /// change events from that account.
        /// </summary>
        public async Task AddCheckingAccount(Guid checkingAccountId)
        {
            // Add the account to our local state (with initial balance of 0)
            _customerState.State.CheckingAccountBalanceById.Add(checkingAccountId, 0);

            // Subscribe to the checking account's balance change stream
            var streamProvider = this.GetStreamProvider("StreamProvider");
            var streamId = StreamId.Create("BalanceStream", checkingAccountId);
            var stream = streamProvider.GetStream<BalanceChangeEvent>(streamId);

            // SubscribeAsync registers this grain as a subscriber
            // The subscription is stored persistently (in PubSubStore)
            await stream.SubscribeAsync(this);

            // Save the updated state
            await _customerState.WriteStateAsync();
        }

        /// <summary>
        /// Calculates net worth by summing cached balances.
        /// Because balances are updated via streams in real-time,
        /// this is a fast, in-memory calculation - no network calls!
        /// </summary>
        public async Task<decimal> GetNetWorth()
        {
            return _customerState.State.CheckingAccountBalanceById.Values.Sum();
        }

        // ======================================================================
        // STREAM OBSERVER METHODS
        // ======================================================================

        /// <summary>Called when the stream completes (no more events).</summary>
        public Task OnCompletedAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>Called when a stream error occurs.</summary>
        public Task OnErrorAsync(Exception ex)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Called when a NEW BALANCE EVENT is published on a stream we're subscribed to.
        /// This is the CORE of the event-driven architecture.
        /// 
        /// Whenever a CheckingAccount is credited or debited, it publishes a
        /// BalanceChangeEvent. This method receives that event and updates
        /// our cached balance for that account.
        /// 
        /// Performance benefit: No polling, no blocking grain calls!
        /// </summary>
        public async Task OnNextAsync(BalanceChangeEvent item, StreamSequenceToken? token = null)
        {
            var checkingAccountBalancesById = _customerState.State.CheckingAccountBalanceById;

            // Update the cached balance for the account mentioned in the event
            if (checkingAccountBalancesById.ContainsKey(item.CheckingAccountId))
            {
                checkingAccountBalancesById[item.CheckingAccountId] = item.Balance;
            }
            else
            {
                // If we don't know about this account yet, add it
                checkingAccountBalancesById.Add(item.CheckingAccountId, item.Balance);
            }

            // Persist the updated balances
            await _customerState.WriteStateAsync();
        }
    }
}
