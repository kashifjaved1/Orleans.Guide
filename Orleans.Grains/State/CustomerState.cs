// ==========================================================================
// CustomerState.cs - Customer Grain Persistent State
// ==========================================================================
// This record stores which checking accounts belong to a customer and
// their LATEST KNOWN BALANCES (updated via streams).
//
// The Dictionary<Guid, decimal> maps:
//   Key   = Checking Account GUID
//   Value = Latest cached balance (updated by stream events)
//
// This is an example of a READ MODEL / PROJECTION pattern:
// Instead of computing net worth by querying every account grain,
// we maintain a pre-computed view that's always up-to-date via events.
// ==========================================================================

namespace Orleans.Grains.State
{
    /// <summary>
    /// Stores a customer's checking accounts and their cached balances.
    /// 
    /// Storage: Azure Table Storage (via "tableStorage" provider)
    /// </summary>
    [GenerateSerializer]
    public record CustomerState
    {
        /// <summary>
        /// Dictionary mapping CheckingAccountId -> Latest Cached Balance.
        /// Balances are updated in real-time via Orleans Stream events.
        /// </summary>
        [Id(0)]
        public Dictionary<Guid, decimal> CheckingAccountBalanceById { get; set; } = new Dictionary<Guid, decimal>();
    }
}
