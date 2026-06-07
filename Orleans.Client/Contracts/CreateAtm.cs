// ==========================================================================
// CreateAtm.cs - Request DTO for Creating an ATM
// ==========================================================================

using System.Runtime.Serialization;

namespace Orleans.Client.Contracts
{
    /// <summary>
    /// Request body for POST /atm
    /// Sent as JSON: { "initialAtmCashBalance": 10000.00 }
    /// </summary>
    [DataContract]
    public record CreateAtm
    {
        /// <summary>How much cash to load into the ATM initially.</summary>
        [DataMember]
        public decimal InitialAtmCashBalance { get; init; }
    }
}
