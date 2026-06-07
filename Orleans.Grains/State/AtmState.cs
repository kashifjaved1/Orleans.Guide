// ==========================================================================
// AtmState.cs - Transactional State for ATM Machine
// ==========================================================================
// This record defines the state for an ATM grain.
// Stored in ITransactionalState<AtmState>, meaning ATM balance changes
// participate in ACID transactions alongside checking account debits.
//
// Multiple [Id] attributes with different numbers:
// Id(0) = first field to serialize
// Id(1) = second field to serialize
// 
// These IDs must be UNIQUE within a type but don't need to be sequential.
// They just need to be stable across versions.
// ==========================================================================

namespace Orleans.Grains.State
{
    /// <summary>
    /// Represents the state of an ATM machine.
    /// 
    /// Storage: Azure Table Storage (transactional)
    /// </summary>
    [GenerateSerializer]
    public record AtmState
    {
        /// <summary>Unique identifier for this ATM (matches the grain key).</summary>
        [Id(0)]
        public Guid Id { get; set; }

        /// <summary>Current cash remaining in the ATM.</summary>
        [Id(1)]
        public decimal Balance { get; set; }
    }
}
