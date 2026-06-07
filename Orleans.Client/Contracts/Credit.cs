// ==========================================================================
// Credit.cs - Request DTO for Depositing Money
// ==========================================================================

using System.Runtime.Serialization;

namespace Orleans.Client.Contracts
{
    /// <summary>
    /// Request body for POST /checkingaccount/{id}/credit
    /// Sent as JSON: { "amount": 100.00 }
    /// </summary>
    [DataContract]
    public record Credit
    {
        /// <summary>The amount to deposit into the account.</summary>
        [DataMember]
        public decimal Amount { get; init; }
    }
}
