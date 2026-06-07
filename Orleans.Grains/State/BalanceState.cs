// ==========================================================================
// BalanceState.cs - Transactional State for Account Balance
// ==========================================================================
// This record defines the state stored by ITransactionalState<BalanceState>
// in CheckingAccountGrain.
//
// ORLEANS SERIALIZATION ATTRIBUTES:
// [GenerateSerializer] - Tells the Orleans code generator to create
//   serialization code for this type. This is REQUIRED for all types
//   that cross grain boundaries (return values, parameters, state).
//
// [Id(0)] - A stable, ordinal identifier for the field. Orleans uses
//   these numeric IDs (NOT field names) for serialization. This means
//   you can RENAME fields without breaking compatibility with existing
//   serialized data! Just keep the same Id number.
//
// WHY RECORDS?
// C# records provide value-based equality and immutability features.
// For state, we use mutable properties (get; set;) because Orleans
// needs to modify the state in-place during PerformUpdate.
// ==========================================================================

namespace Orleans.Grains.State
{
    /// <summary>
    /// Represents the balance of a checking account.
    /// This is stored in TRANSACTIONAL state, meaning it participates
    /// in ACID transactions. If a transaction fails, the balance is
    /// automatically rolled back.
    /// 
    /// Storage: Azure Table Storage (via AzureTableTransactionalStateStorage)
    /// </summary>
    [GenerateSerializer]
    public record BalanceState
    {
        /// <summary>
        /// The current account balance.
        /// Id(0) is the stable serialization identifier.
        /// </summary>
        [Id(0)]
        public decimal Balance { get; set; }
    }
}
