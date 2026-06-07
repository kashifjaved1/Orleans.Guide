// ==========================================================================
// BalanceChangeEvent.cs - Stream Event for Balance Updates
// ==========================================================================
// This is an EVENT - a message published to an Orleans Stream whenever
// a checking account balance changes.
//
// EVENTS VS COMMANDS:
// - Command: "Debit $50 from account X" (an instruction)
// - Event:  "Account X balance changed to $150" (a fact that happened)
//
// Events are immutable facts about the past. The "init" keyword enforces
// that the event's values can only be set during initialization (constructor
// or object initializer) and cannot be changed afterward.
//
// STREAM EVENT FLOW:
// 1. CheckingAccountGrain.Credit/Debit runs
// 2. It publishes a BalanceChangeEvent to the stream
// 3. All subscribers (like CustomerGrain) receive the event
// 4. Subscribers update their own state based on the event
// 
// This is an EVENT-DRIVEN ARCHITECTURE pattern!
// ==========================================================================

using System.Runtime.Serialization;

namespace Orleans.Grains.Events
{
    /// <summary>
    /// Published to Orleans Streams whenever a checking account balance changes.
    /// Subscribers (like CustomerGrain) use these events to keep their
    /// cached state up-to-date.
    /// </summary>
    [GenerateSerializer]
    public record BalanceChangeEvent
    {
        /// <summary>Which checking account's balance changed.</summary>
        [Id(0)]
        public Guid CheckingAccountId { get; init; }

        /// <summary>The new balance after the change.</summary>
        [Id(1)]
        public decimal Balance { get; init; }
    }
}
