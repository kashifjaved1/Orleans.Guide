// ==========================================================================
// RecurringPayment.cs - Recurring Payment Configuration
// ==========================================================================
// Models a recurring payment (like a Netflix subscription, gym membership,
// or mortgage payment) that automatically debits the account on a schedule.
//
// This is stored as part of CheckingAccountState.RecurringPayments list
// and drives the Orleans Reminder system.
// ==========================================================================

namespace Orleans.Grains.State
{
    /// <summary>
    /// Configuration for a single recurring payment.
    /// </summary>
    [GenerateSerializer]
    public record RecurringPayment
    {
        /// <summary>Unique identifier for this payment configuration.</summary>
        [Id(0)]
        public Guid PaymentId { get; set; }

        /// <summary>Amount to deduct each time the payment fires.</summary>
        [Id(1)]
        public decimal PaymentAmount { get; set; }

        /// <summary>How often the payment recurs (in minutes).</summary>
        [Id(2)]
        public int OccursEveryMinutes { get; set; }
    }
}
