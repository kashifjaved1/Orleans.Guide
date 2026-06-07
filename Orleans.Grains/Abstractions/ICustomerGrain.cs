// ==========================================================================
// ICustomerGrain.cs - Customer Grain Interface
// ==========================================================================
// This grain represents a BANK CUSTOMER. It demonstrates how to aggregate
// information across MULTIPLE related grains using Orleans STREAMS.
//
// KEY LEARNING - GRAIN RELATIONSHIPS:
// In this banking domain:
//   - A Customer has multiple Checking Accounts
//   - Each Checking Account is its own grain
//   - The Customer grain tracks which accounts belong to the customer
//   - Using Orleans Streams, the Customer grain receives REAL-TIME updates
//     whenever any of their account balances change
//
// This is more efficient than polling - instead of asking each account
// "what's your balance?" repeatedly, the customer grain just LISTENS
// for balance change events.
// ==========================================================================

namespace Orleans.Grains.Abstractions
{
    /// <summary>
    /// Defines the operations available on a Customer grain.
    /// 
    /// Unlike CheckingAccountGrain and AtmGrain, this grain does NOT use
    /// transactions. Why? Because it doesn't directly modify the actual
    /// money balances - it just tracks which accounts belong to the customer
    /// and listens for balance updates via streams.
    /// </summary>
    public interface ICustomerGrain : IGrainWithGuidKey
    {
        /// <summary>
        /// Links a checking account to this customer.
        /// After calling this, the customer grain will subscribe to
        /// balance change events from that account via streams.
        /// 
        /// This is like saying "this checking account belongs to this customer"
        /// in the bank's database.
        /// </summary>
        Task AddCheckingAccount(Guid checkingAccountId);

        /// <summary>
        /// Calculates the customer's total net worth by summing up
        /// the cached balances of all their checking accounts.
        /// 
        /// The balances are kept up-to-date via stream events, so this
        /// is a fast in-memory calculation - no grain calls needed!
        /// </summary>
        Task<decimal> GetNetWorth();
    }
}
