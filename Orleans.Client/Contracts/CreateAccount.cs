// ==========================================================================
// CreateAccount.cs - Request DTO for Creating a Checking Account
// ==========================================================================
// DTO (Data Transfer Object): A simple object that carries data between
// the HTTP client and the API. No logic, just data.
//
// [DataContract] / [DataMember]: These .NET attributes control how the
// object is serialized to/from formats like JSON or XML. They're used here
// because ASP.NET Core uses them for model binding in API requests.
//
// "init" keyword: C# 9+ feature that allows property values to be set
// only during object initialization (constructor or object initializer).
// This supports immutable DTOs.
// ==========================================================================

using System.Runtime.Serialization;

namespace Orleans.Client.Contracts
{
    /// <summary>
    /// Request body for POST /checkingaccount
    /// Sent as JSON: { "openingBalance": 1000.00 }
    /// </summary>
    [DataContract]
    public record CreateAccount
    {
        /// <summary>The initial deposit amount to open the account with.</summary>
        [DataMember]
        public decimal OpeningBalance { get; init; }
    }
}
