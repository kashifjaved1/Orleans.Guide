// ==========================================================================
// Orleans.Client/Program.cs - THE CLIENT (API) ENTRY POINT
// ==========================================================================
//
// WHAT IS AN ORLEANS CLIENT?
// ---------------------------------------------------------------
// The Orleans Client is a process that connects to an Orleans Cluster
// and sends requests to grains. It does NOT host grains itself.
//
// In this project, the client is an ASP.NET Core Web API that:
// 1. Connects to the Orleans cluster (using same config as the silo)
// 2. Exposes REST endpoints for users to interact with
// 3. Translates HTTP requests into grain method calls
//
// ARCHITECTURE:
//   HTTP Client --> ASP.NET Core API --> Orleans Client --> Grain
//
// The client injects IClusterClient (the connection to the cluster)
// and ITransactionClient (for transactional operations) into the
// Minimal API endpoints via DI.
// ==========================================================================

using Orleans.Client.Contracts;
using Orleans.Configuration;
using Orleans.Grains.Abstractions;
using Orleans.Grains.Filters;

var builder = WebApplication.CreateBuilder(args);

// ==========================================================================
// CONFIGURE THE ORLEANS CLIENT
// ==========================================================================
// builder.Host.UseOrleansClient() configures how this application connects
// to the Orleans cluster. The configuration must MATCH the silo's config
// (same ClusterId, ServiceId, and clustering mechanism).
// ==========================================================================
builder.Host.UseOrleansClient((context, client) =>
{
    // Must match Silo's clustering configuration
    client.UseAzureStorageClustering(configureOptions: options =>
    {
        options.TableServiceClient = new Azure.Data.Tables.TableServiceClient("UseDevelopmentStorage=true;");
    });

    // Must match Silo's cluster identity
    client.Configure<ClusterOptions>(options =>
    {
        options.ClusterId = "practiceCluster";
        options.ServiceId = "practiceService";
    });

    // OUTGOING FILTER: Intercepts requests FROM this client TO grains
    client.AddOutgoingGrainCallFilter<LoggingOutgoingGrainCallFilter>();

    // Enable transaction support on the client side
    client.UseTransactions();
});

var app = builder.Build();

// ==========================================================================
// WHAT IS ICLUSTERCLIENT?
// ---------------------------------------------------------------
// IClusterClient is the main entry point for communicating with grains.
// It is automatically injected into Minimal API endpoints.
//
//   clusterClient.GetGrain<IGrainInterface>(key)
//
// This returns a PROXY to the grain - a lightweight object that forwards
// all calls to the actual grain running on some silo in the cluster.
// The proxy handles serialization, routing, load balancing, and retries.
//
// WHAT IS ITRANSACTIONCLIENT?
// ---------------------------------------------------------------
// ITransactionClient is used to create transactions from OUTSIDE a grain.
// It's injected by calling .UseTransactions() on the client builder.
//
//   transactionClient.RunTransaction(TransactionOption.Create, async () => { ... })
//
// This creates an ACID transaction that can span MULTIPLE grain calls.
// ==========================================================================

// ======================================================================
// ENDPOINT 1: GET /checkingaccount/{checkingAccountId}/balance
// ======================================================================
// Demonstrates: Basic read from grain state within a transaction.
//
// HTTP: GET http://localhost:5297/checkingaccount/{guid}/balance
// Response: 200 OK with decimal balance
// ======================================================================
app.MapGet("checkingaccount/{checkingAccountId}/balance",
    async (Guid checkingAccountId, ITransactionClient transactionClient, IClusterClient clusterClient) =>
    {
        decimal balance = 0;

        // TransactionOption.Create: Start a NEW transaction.
        // The transaction wraps the grain call so we get a consistent read.
        await transactionClient.RunTransaction(TransactionOption.Create, async () =>
        {
            // GetGrain: Gets a proxy to the CheckingAccountGrain with this ID.
            // If the grain doesn't exist yet, Orleans will create it on demand
            // (this is the "virtual" aspect of virtual actors).
            var checkingAccountGrain = clusterClient.GetGrain<ICheckingAccountGrain>(checkingAccountId);

            // The actual network call to the grain happens here.
            // Orleans serializes the method call, sends it to the silo hosting
            // this grain, executes it, and returns the result.
            balance = await checkingAccountGrain.GetBalance();
        });

        return TypedResults.Ok(balance);
    });

// ======================================================================
// ENDPOINT 2: POST /checkingaccount
// ======================================================================
// Demonstrates: Creating a new grain with initial state.
//
// HTTP: POST http://localhost:5297/checkingaccount
// Body: { "openingBalance": 1000.00 }
// Response: 201 Created with location header
// ======================================================================
app.MapPost("checkingaccount", async (CreateAccount createAccount,
    ITransactionClient transactionClient, IClusterClient clusterClient) =>
{
    // Generate a new GUID for this account
    // This GUID becomes the grain's identity (primary key)
    var checkingAccountId = Guid.NewGuid();

    await transactionClient.RunTransaction(TransactionOption.Create, async () =>
    {
        // GetGrain with a NEW Guid: Orleans creates a new grain instance
        var checkingAccountGrain = clusterClient.GetGrain<ICheckingAccountGrain>(checkingAccountId);
        await checkingAccountGrain.Initialise(createAccount.OpeningBalance);
    });

    return TypedResults.Created($"checkingaccount/{checkingAccountId}", null);
});

// ======================================================================
// ENDPOINT 3: POST /checkingaccount/{checkingAccountId}/debit
// ======================================================================
// Demonstrates: Writing to transactional state.
//
// HTTP: POST http://localhost:5297/checkingaccount/{guid}/debit
// Body: { "amount": 50.00 }
// Response: 204 No Content
// ======================================================================
app.MapPost("checkingaccount/{checkingAccountId}/debit", async (Guid checkingAccountId,
    Debit debit, ITransactionClient transactionClient, IClusterClient clusterClient) =>
{
    await transactionClient.RunTransaction(TransactionOption.Create, async () =>
    {
        var checkingAccountGrain = clusterClient.GetGrain<ICheckingAccountGrain>(checkingAccountId);
        await checkingAccountGrain.Debit(debit.Amount);
    });

    return TypedResults.NoContent();
});

// ======================================================================
// ENDPOINT 4: POST /checkingaccount/{checkingAccountId}/credit
// ======================================================================
// Demonstrates: Writing to transactional state.
//
// HTTP: POST http://localhost:5297/checkingaccount/{guid}/credit
// Body: { "amount": 100.00 }
// Response: 204 No Content
// ======================================================================
app.MapPost("checkingaccount/{checkingAccountId}/credit", async (Guid checkingAccountId,
    Credit credit, ITransactionClient transactionClient, IClusterClient clusterClient) =>
{
    await transactionClient.RunTransaction(TransactionOption.Create, async () =>
    {
        var checkingAccountGrain = clusterClient.GetGrain<ICheckingAccountGrain>(checkingAccountId);
        await checkingAccountGrain.Credit(credit.Amount);
    });

    return TypedResults.NoContent();
});

// ======================================================================
// ENDPOINT 5: POST /checkingaccount/{checkingAccountId}/recurringPayment
// ======================================================================
// Demonstrates: Using Orleans Reminders for scheduled tasks.
// This does NOT use a transaction because it's just setting up
// configuration, not modifying the actual balance.
//
// HTTP: POST http://localhost:5297/checkingaccount/{guid}/recurringPayment
// Body: { "paymentId": "guid", "paymentAmount": 9.99, "paymentRecurrsEveryMinutes": 1 }
// Response: 204 No Content
// ======================================================================
app.MapPost("checkingaccount/{checkingAccountId}/recurringPayment", async (Guid checkingAccountId,
    CreateRecurringPayment createRecurringPayment, IClusterClient clusterClient) =>
{
    var checkingAccountGrain = clusterClient.GetGrain<ICheckingAccountGrain>(checkingAccountId);
    await checkingAccountGrain.AddReccuringPayment(
        createRecurringPayment.PaymentId,
        createRecurringPayment.PaymentAmount,
        createRecurringPayment.PaymentRecurrsEveryMinutes);
    return TypedResults.NoContent();
});

// ======================================================================
// ENDPOINT 6: POST /checkingaccount/{checkingAccountId}/fireandforgetwork
// ======================================================================
// Demonstrates: [OneWay] Fire-and-Forget calls.
// The method is marked [OneWay], so the Task returns IMMEDIATELY even
// though the grain method takes 2 seconds and THROWS an exception.
//
// HTTP: POST http://localhost:5297/checkingaccount/{guid}/fireandforgetwork
// Response: 204 No Content (immediately, before grain finishes)
// ======================================================================
app.MapPost("checkingaccount/{checkingAccountId}/fireandforgetwork", async (Guid checkingAccountId,
    IClusterClient clusterClient) =>
{
    var checkingAccountGrain = clusterClient.GetGrain<ICheckingAccountGrain>(checkingAccountId);
    await checkingAccountGrain.FireAndForgetWork();
    return TypedResults.NoContent();
});

// ======================================================================
// ENDPOINT 7: POST /checkingaccount/{checkingAccountId}/cancellablework
// ======================================================================
// Demonstrates: GrainCancellationToken - cancelling long-running grain work.
//
// HOW CANCELLATION WORKS:
// 1. Client creates a GrainCancellationTokenSource
// 2. Client passes the token to the grain method
// 3. If client calls Cancel() on the source, the grain gets a cancellation signal
// 4. The grain method can check the token and stop gracefully
//
// NOTE: The cancellation code is commented out below. Uncomment it to
// see cancellation in action (the grain will stop after 5 seconds instead
// of running for the full 15 seconds).
//
// HTTP: POST http://localhost:5297/checkingaccount/{guid}/cancellablework
// Response: 204 No Content
// ======================================================================
app.MapPost("checkingaccount/{checkingAccountId}/cancellablework", async (Guid checkingAccountId,
    IClusterClient clusterClient) =>
{
    var checkingAccountGrain = clusterClient.GetGrain<ICheckingAccountGrain>(checkingAccountId);
    var grainCancellationTokenSource = new GrainCancellationTokenSource();
    var grainCallTask = checkingAccountGrain.CancelableWork(grainCancellationTokenSource.Token, 15);

    // Uncomment this block to test cancellation after 5 seconds:
    //var cancelWorkTask = async () =>
    //{
    //    await Task.Delay(TimeSpan.FromSeconds(5));
    //    await grainCancellationTokenSource.Cancel();
    //};

    await Task.WhenAll(grainCallTask);
    return TypedResults.NoContent();
});

// ======================================================================
// ENDPOINT 8: POST /atm
// ======================================================================
// Creates a new ATM with initial cash.
//
// HTTP: POST http://localhost:5297/atm
// Body: { "initialAtmCashBalance": 10000.00 }
// Response: 201 Created with location header
// ======================================================================
app.MapPost("atm", async (CreateAtm createAtm,
    ITransactionClient transactionClient, IClusterClient clusterClient) =>
{
    var atmId = Guid.NewGuid();
    await transactionClient.RunTransaction(TransactionOption.Create, async () =>
    {
        var atmGrain = clusterClient.GetGrain<IAtmGrain>(atmId);
        await atmGrain.Initialise(createAtm.InitialAtmCashBalance);
    });
    return TypedResults.Created($"atm/{atmId}", null);
});

// ======================================================================
// ENDPOINT 9: GET /atm/{atmId}/balance
// ======================================================================
// Gets the remaining cash in an ATM.
//
// HTTP: GET http://localhost:5297/atm/{guid}/balance
// Response: 200 OK with decimal ATM balance
// ======================================================================
app.MapGet("atm/{atmId}/balance", async (Guid atmId,
    ITransactionClient transactionClient, IClusterClient clusterClient) =>
{
    decimal balance = 0;
    await transactionClient.RunTransaction(TransactionOption.Create, async () =>
    {
        var atmGrain = clusterClient.GetGrain<IAtmGrain>(atmId);
        balance = await atmGrain.GetBalance();
    });
    return TypedResults.Ok(balance);
});

// ======================================================================
// ENDPOINT 10: POST /atm/{atmId}/withdrawl
// ======================================================================
// Demonstrates: MULTI-GRAIN TRANSACTION.
// This endpoint updates BOTH the ATM grain AND the CheckingAccount grain
// within a single atomic transaction.
//
// THE TRANSACTION GUARANTEES:
// - ATM cash decreases AND account balance decreases
// - If the account debit fails, the ATM cash decrease is ROLLED BACK
// - If the ATM update fails, the account debit is never attempted
// - Both succeed OR both fail - never one without the other
//
// HTTP: POST http://localhost:5297/atm/{guid}/withdrawl
// Body: { "checkingAccountId": "guid", "amount": 50.00 }
// Response: No explicit return (204 No Content implicitly)
// ======================================================================
app.MapPost("atm/{atmId}/withdrawl", async (Guid atmId, AtmWithdrawl atmWithdrawl,
    ITransactionClient transactionClient, IClusterClient clusterClient) =>
{
    // Single transaction wrapping two grain operations
    await transactionClient.RunTransaction(TransactionOption.Create, async () =>
    {
        // 1. Reduce cash in the ATM
        var atmGrain = clusterClient.GetGrain<IAtmGrain>(atmId);
        var checkingAccountGrain = clusterClient.GetGrain<ICheckingAccountGrain>(atmWithdrawl.CheckingAccountId);

        // 2. Debit the customer's checking account
        await atmGrain.Withdraw(atmWithdrawl.CheckingAccountId, atmWithdrawl.Amount);
        await checkingAccountGrain.Debit(atmWithdrawl.Amount);
    });
});

// ======================================================================
// ENDPOINT 11: GET /customer/{customerId}/networth
// ======================================================================
// Demonstrates: Stream-based read model (cached, event-driven balance).
// This does NOT call each account grain - it reads the cached sum from
// the CustomerGrain's state (updated via streams).
//
// HTTP: GET http://localhost:5297/customer/{guid}/networth
// Response: 200 OK with decimal net worth
// ======================================================================
app.MapGet("customer/{customerId}/networth", async (Guid customerId, IClusterClient clusterClient) =>
{
    var customerGrain = clusterClient.GetGrain<ICustomerGrain>(customerId);
    var netWorth = await customerGrain.GetNetWorth();
    return TypedResults.Ok(netWorth);
});

// ======================================================================
// ENDPOINT 12: POST /customer/{customerId}/addcheckingaccount
// ======================================================================
// Links a checking account to a customer (and subscribes to streams).
//
// HTTP: POST http://localhost:5297/customer/{guid}/addcheckingaccount
// Body: { "accountId": "guid" }
// Response: 204 No Content
// ======================================================================
app.MapPost("customer/{customerId}/addcheckingaccount", async (Guid customerId,
    CustomerCheckingAccount customerCheckingAccount, IClusterClient clusterClient) =>
{
    var customerGrain = clusterClient.GetGrain<ICustomerGrain>(customerId);
    await customerGrain.AddCheckingAccount(customerCheckingAccount.AccountId);
    return TypedResults.NoContent();
});

// ======================================================================
// ENDPOINT 13: POST /transfer
// ======================================================================
// Demonstrates: StatelessWorker orchestrator pattern.
// The orchestrator grain handles the debit/credit coordination.
//
// This also demonstrates that a grain can create transactions internally
// using ITransactionClient - the client doesn't need to create the
// transaction.
//
// HTTP: POST http://localhost:5297/transfer
// Body: { "fromAccountId": "guid", "toAccountId": "guid", "amount": 100.00 }
// Response: 204 No Content
// ======================================================================
app.MapPost("transfer", async (Transfer transfer, IClusterClient clusterClient) =>
{
    // Note: Key is "0" (integer) because this grain uses IGrainWithIntegerKey
    var statlessTransferProcessingGrain = clusterClient.GetGrain<IStatlessTransferProcessingGrain>(0);
    await statlessTransferProcessingGrain.ProcessTransfer(
        transfer.FromAccountId, transfer.ToAccountId, transfer.Amount);
    return TypedResults.NoContent();
});

app.Run();
