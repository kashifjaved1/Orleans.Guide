// ==========================================================================
// AtmWithdrawl.cs - Request DTO for ATM Withdrawal
// ==========================================================================

using System.Runtime.Serialization;

namespace Orleans.Client.Contracts
{
    /// <summary>
    /// Request body for POST /atm/{atmId}/withdrawl
    /// Sent as JSON: { "checkingAccountId": "guid-here", "amount": 50.00 }
    /// </summary>
    [DataContract]
    public record AtmWithdrawl
    {
        /// <summary>The customer's checking account to debit.</summary>
        [DataMember]
        public Guid CheckingAccountId { get; init; }

        /// <summary>How much money to withdraw.</summary>
        [DataMember]
        public decimal Amount { get; init; }
    }
}
