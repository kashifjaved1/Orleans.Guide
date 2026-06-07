// ==========================================================================
// Transfer.cs - Request DTO for Money Transfers
// ==========================================================================

using System.Runtime.Serialization;

namespace Orleans.Client.Contracts
{
    /// <summary>
    /// Request body for POST /transfer
    /// Sent as JSON: { "fromAccountId": "guid", "toAccountId": "guid", "amount": 100.00 }
    /// </summary>
    [DataContract]
    public record Transfer
    {
        /// <summary>Which account to take money FROM.</summary>
        [DataMember]
        public Guid ToAccountId { get; init; }

        /// <summary>Which account to send money TO.</summary>
        [DataMember]
        public Guid FromAccountId { get; init; }

        /// <summary>How much money to transfer.</summary>
        [DataMember]
        public decimal Amount { get; init; }
    }
}
