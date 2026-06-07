// ==========================================================================
// Debit.cs - Request DTO for Withdrawing Money
// ==========================================================================

using System.Runtime.Serialization;

namespace Orleans.Client.Contracts
{
    /// <summary>
    /// Request body for POST /checkingaccount/{id}/debit
    /// Sent as JSON: { "amount": 50.00 }
    /// </summary>
    [DataContract]
    public record Debit
    {
        /// <summary>The amount to withdraw from the account.</summary>
        [DataMember]
        public decimal Amount { get; init; }
    }
}
