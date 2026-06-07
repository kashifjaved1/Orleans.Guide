// ==========================================================================
// CustomerCheckingAccount.cs - Request DTO for Linking Account to Customer
// ==========================================================================

using System.Runtime.Serialization;

namespace Orleans.Client.Contracts
{
    /// <summary>
    /// Request body for POST /customer/{id}/addcheckingaccount
    /// Sent as JSON: { "accountId": "guid-here" }
    /// </summary>
    [DataContract]
    public record CustomerCheckingAccount
    {
        /// <summary>The checking account GUID to link to this customer.</summary>
        [DataMember]
        public Guid AccountId { get; init; }
    }
}
