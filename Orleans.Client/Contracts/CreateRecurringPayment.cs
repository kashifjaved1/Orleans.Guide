// ==========================================================================
// CreateRecurringPayment.cs - Request DTO for Recurring Payments
// ==========================================================================

using System.Runtime.Serialization;

namespace Orleans.Client.Contracts
{
    /// <summary>
    /// Request body for POST /checkingaccount/{id}/recurringPayment
    /// Sent as JSON: { "paymentId": "guid", "paymentAmount": 9.99, "paymentRecurrsEveryMinutes": 43200 }
    /// </summary>
    [DataContract]
    public record CreateRecurringPayment
    {
        /// <summary>Unique ID for this recurring payment configuration.</summary>
        [DataMember]
        public Guid PaymentId { get; init; }

        /// <summary>Amount to automatically debit each period.</summary>
        [DataMember] 
        public decimal PaymentAmount { get; init; }

        /// <summary>How often the payment should recur, in minutes.</summary>
        [DataMember]
        public int PaymentRecurrsEveryMinutes { get; init; }
    }
}
