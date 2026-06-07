# Orleans.Practice — A Complete .NET Orleans Banking Demo 🏦

> **Learn Microsoft Orleans by building a real banking system. From zero to distributed systems hero.**

| .NET Version | Orleans Version | Azure Storage |
|-------------|----------------|---------------|
| .NET 9.0    | 10.1.0         | Azurite (Local Emulator) |

---

## 📚 Table of Contents

1. [What is This Project?](#-what-is-this-project)
2. [What is Microsoft Orleans?](#-what-is-microsoft-orleans)
3. [Project Architecture](#-project-architecture)
4. [The Banking Domain — A Perfect Analogy](#-the-banking-domain--a-perfect-analogy)
5. [Prerequisites](#-prerequisites)
6. [Getting Started in 5 Minutes](#-getting-started-in-5-minutes)
7. [Project Structure — Every File Explained](#-project-structure--every-file-explained)
8. [Orleans Concepts — Deep Dive](#-orleans-concepts--deep-dive)
9. [API Reference — All 13 Endpoints](#-api-reference--all-13-endpoints)
10. [Testing the API](#-testing-the-api)
11. [Common Issues & Troubleshooting](#-common-issues--troubleshooting)
12. [Going Further](#-going-further)

---

## 🎯 What is This Project?

This is a **learning-oriented** demo project that teaches **[Microsoft Orleans](https://learn.microsoft.com/en-us/dotnet/orleans/)** — Microsoft's **Virtual Actor framework** for building distributed, highly-scalable applications.

The project simulates a **realistic banking system** with:

| Feature | Orleans Concept Demonstrated |
|---------|------------------------------|
| Checking accounts | **Grains** (Virtual Actors) |
| ATM machines | **Grains** with their own state |
| Deposits & withdrawals | **ACID Transactions** across grains |
| Money transfers between accounts | **Stateless Worker Orchestrators** |
| Recurring bill payments (auto-debit) | **Reminders** (persistent timers) |
| Real-time balance updates | **Streams** (pub-sub event system) |
| Customer net worth tracking | **Stream Subscriptions** (event-driven read models) |
| Fire-and-forget operations | **OneWay** calls |
| Cancelling long-running operations | **Grain Cancellation Tokens** |
| Request logging | **Grain Call Filters** |
| Safe concurrent access | **Reentrant** grains |

---

## 🤔 What is Microsoft Orleans?

### The Problem Orleans Solves

Building distributed applications is HARD. You need to deal with:

- **Concurrency** — How do multiple users access the same data safely?
- **State management** — Where does data live? In memory? In a database?
- **Fault tolerance** — What happens when a server crashes?
- **Scalability** — How do you go from 10 users to 10 million?
- **Location transparency** — How does code on machine A call code on machine B?

Traditional approaches use databases, locks, queues, message brokers, and complex infrastructure. Orleans simplifies all of this.

### The Virtual Actor Model

Orleans is based on the **Actor Model** — a programming paradigm where:

- **Actors** (called **Grains** in Orleans) are the fundamental units of computation
- Each actor has a **unique identity** (like a database primary key)
- Each actor has **private state** (its own data)
- Actors communicate via **asynchronous messages**
- Each actor processes messages **one at a time** (single-threaded)

### What Makes Orleans Special? (The "Virtual" Part)

Traditional actor frameworks require you to **explicitly create, destroy, and manage** actor lifecycles. Orleans uses **Virtual Actors** — grains that:

- **Always exist conceptually** — You can call `GetGrain<IFoo>(key)` even if the grain has never been created
- **Are created automatically** on first use (lazy activation)
- **Are garbage collected** when idle (automatic deactivation)
- **Are reactivated** when needed again (transparent to you)
- **Can move between servers** — Orleans handles migration

> **Analogy:** Virtual grains are like URLs. `GetGrain<IUserGrain>(userId)` is like typing a URL — the resource exists at that address, and the infrastructure handles serving it. You don't worry about which server it's on.

### Orleans Terminology Cheat Sheet

| Term | What It Is | Real-World Analogy |
|------|-----------|-------------------|
| **Grain** | A virtual actor — an object with identity, state, and behavior | A bank teller who only serves one account |
| **Silo** | A server process that hosts grains | A bank branch building |
| **Cluster** | A group of silos working together | The entire bank chain |
| **Client** | An application that sends requests to grains | A customer at the bank |
| **Grain Interface** | The public contract of a grain (what it can do) | The services listed on a bank's website |
| **Grain Key** | The unique ID of a grain instance | Your account number |
| **Activation** | A running instance of a grain in memory | A teller actively serving your request |
| **Persistence** | Saving grain state to durable storage | The bank's record-keeping system |

---

## 🏗️ Project Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                        YOUR COMPUTER                                │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  PROCESS 1: Orleans.Client (ASP.NET Web API)                │   │
│  │                                                             │   │
│  │  ┌──────────────────────┐    ┌──────────────────────────┐   │   │
│  │  │  GET/POST endpoints  │───▶│  Orleans IClusterClient  │───┼───┼──▶
│  │  └──────────────────────┘    └──────────────────────────┘   │   │   │
│  └─────────────────────────────────────────────────────────────┘   │   │
│                                                                     │   │
│  ┌─────────────────────────────────────────────────────────────┐   │   │
│  │  PROCESS 2: Orleans.Silo (Console App)                      │   │   │
│  │                                                             │   │   │
│  │  ┌──────────────────────────────────────────────────┐       │   │   │
│  │  │  Orleans Silo Host                               │       │   │   │
│  │  │  ┌──────────────┐  ┌──────────────┐              │       │   │   │
│  │  │  │ CheckingAcct │  │   AtmGrain   │  ...         │       │   │   │
│  │  │  │   Grain      │  │              │              │       │   │   │
│  │  │  └──────────────┘  └──────────────┘              │       │   │   │
│  │  └──────────────────────────────────────────────────┘       │   │   │
│  └─────────────────────────────────────────────────────────────┘   │   │
│                                                                     │   │
│  ┌─────────────────────────────────────────────────────────────┐   │   │
│  │  PROCESS 3: Azurite (Azure Storage Emulator)                │   │   │
│  │                                                             │   │   │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │   │   │
│  │  │  Table Store  │  │  Blob Store  │  │  Queue Store │      │   │   │
│  │  │  (struct'd)   │  │  (documents) │  │  (messages)  │      │   │   │
│  │  └──────────────┘  └──────────────┘  └──────────────┘      │   │   │
│  └─────────────────────────────────────────────────────────────┘   │   │
└─────────────────────────────────────────────────────────────────────┘
```

### The Three Projects

| Project | Type | Role |
|---------|------|------|
| **Orleans.Grains** | Class Library | Contains all grain interfaces, implementations, state, events, and filters |
| **Orleans.Silo** | Console App | Runs the Orleans server that hosts grains |
| **Orleans.Client** | ASP.NET Web API | Connects to the cluster and exposes REST endpoints |

### How They Communicate

```
HTTP Request
    │
    ▼
Orleans.Client (process 1)
    │  Uses IClusterClient to send grain calls
    ▼
Orleans.Silo (process 2)
    │  Hosts grain instances, executes grain methods
    ▼
Azure Storage (process 3, Azurite)
    │  Persists state, reminders, stream events, cluster membership
    ▼
Data is saved
```

**Critical:** The Client and Silo are **separate processes**. You must run BOTH for the system to work. You also need Azurite running for storage.

---

## 🏦 The Banking Domain — A Perfect Analogy

Orleans' actor model maps naturally to real-world banking:

| Real World | Orleans Equivalent |
|-----------|-------------------|
| **Each checking account** has its own balance, transactions, and account holder | `CheckingAccountGrain` — a grain with its own state and methods |
| **Each ATM machine** has its own cash inventory | `AtmGrain` — a grain with its own cash balance |
| **Each customer** has a portfolio of accounts | `CustomerGrain` — a grain that tracks owned accounts |
| **A transfer** requires debiting one account and crediting another atomically | A transaction spanning multiple grains |
| **A recurring bill payment** happens automatically on a schedule | An Orleans Reminder that fires periodically |
| **When you check your net worth**, you want an instant answer | Streams keep cached balances up-to-date |
| **Bank staff** log every transaction for auditing | Grain Call Filters intercept every request |

---

## 📋 Prerequisites

Before running this project, install:

| Tool | Version | Why |
|------|---------|-----|
| [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) | 9.0+ | The project targets `net9.0` |
| [Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite) | Latest | Local Azure Storage emulator (required) |
| [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/) | Latest | Recommended IDE |

### Installing Azurite

Azurite emulates Azure Storage (Tables, Blobs, Queues) locally. This project uses `"UseDevelopmentStorage=true;"` which points to Azurite's default ports.

**Option A: npm** (easiest)
```bash
npm install -g azurite
azurite --silent
```

**Option B: Docker**
```bash
docker run -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite
```

**Option C: Visual Studio** — Azurite is included with Visual Studio 2022. Look for the Azurite icon in the status bar or use View → Other Windows → Azurite Explorer.

---

## 🚀 Getting Started in 5 Minutes

### Step 1: Start Azurite
```bash
azurite --silent
```
Keep this running in a terminal window. You should see:
```
Azurite Blob service is starting at http://127.0.0.1:10000
Azurite Table service is starting at http://127.0.0.1:10002
Azurite Queue service is starting at http://127.0.0.1:10001
```

### Step 2: Start the Silo (Server)
Open a **new** terminal:
```bash
cd Orleans.Silo
dotnet run
```
Wait for output like:
```
[INF] Orleans Silo started.
```
The silo connects to Azurite, registers itself in the cluster, and starts listening for grain requests. **Keep this running.**

### Step 3: Start the Client (API)
Open **another** terminal:
```bash
cd Orleans.Client
dotnet run
```
The API starts on `http://localhost:5297`. You should see:
```
Now listening on: http://localhost:5297
```

### Step 4: Test It!
```bash
# Create a checking account
curl -X POST http://localhost:5297/checkingaccount \
  -H "Content-Type: application/json" \
  -d '{"openingBalance": 1000.00}'
# Returns: 201 Created with Location header

# (Grab the GUID from the Location header and use it below)

# Check the balance
curl http://localhost:5297/checkingaccount/{guid}/balance
# Returns: 1000.00

# Deposit money
curl -X POST http://localhost:5297/checkingaccount/{guid}/credit \
  -H "Content-Type: application/json" \
  -d '{"amount": 500.00}'

# Check balance again
curl http://localhost:5297/checkingaccount/{guid}/balance
# Returns: 1500.00
```

---

## 📁 Project Structure — Every File Explained

```
Orleans.Practice/
│
├── Orleans.Practice.sln          # Visual Studio Solution file
│
├── Orleans.Grains/                # ★ THE CORE: Grain library
│   ├── Orleans.Grains.csproj      # Project file (class library)
│   │
│   ├── Abstractions/              # Grain INTERFACES (public contracts)
│   │   ├── ICheckingAccountGrain.cs    # Interface for checking accounts
│   │   ├── IAtmGrain.cs               # Interface for ATMs
│   │   ├── ICustomerGrain.cs           # Interface for customers
│   │   └── IStatlessTransferProcessingGrain.cs  # Interface for transfers
│   │
│   ├── Grains/                    # Grain IMPLEMENTATIONS (actual logic)
│   │   ├── CheckingAccountGrain.cs     # ★ Main grain: balance, debit, credit
│   │   ├── AtmGrain.cs                # ATM with cash inventory
│   │   ├── CustomerGrain.cs           # Customer with stream subscriptions
│   │   └── StatlessTransferProcessingGrain.cs  # Transfer orchestrator
│   │
│   ├── State/                     # State classes (persisted data models)
│   │   ├── BalanceState.cs             # Balance (transactional)
│   │   ├── AtmState.cs                 # ATM state (transactional)
│   │   ├── CheckingAccountState.cs     # Account metadata (persistent)
│   │   ├── CustomerState.cs            # Customer's account list (persistent)
│   │   ├── RecurringPayment.cs         # Recurring payment config
│   │   └── TransferState.cs            # Transfer counter
│   │
│   ├── Events/                    # Stream event types
│   │   └── BalanceChangeEvent.cs       # Published when balance changes
│   │
│   └── Filters/                   # Grain call interceptors
│       ├── LoggingIncomingGrainCallFilter.cs  # Silo-side logging
│       └── LoggingOutgoingGrainCallFilter.cs  # Client-side logging
│
├── Orleans.Silo/                  # ★ THE SERVER: hosts grains
│   ├── Orleans.Silo.csproj        # Project file (console app)
│   └── Program.cs                 # Silo configuration & startup
│
└── Orleans.Client/                # ★ THE CLIENT: REST API
    ├── Orleans.Client.csproj      # Project file (web app)
    ├── Program.cs                 # API endpoints & client config
    ├── appsettings.json           # ASP.NET config
    ├── Properties/
    │   └── launchSettings.json    # Dev server settings
    └── Contracts/                 # Request/Response DTOs
        ├── CreateAccount.cs
        ├── Debit.cs
        ├── Credit.cs
        ├── CreateAtm.cs
        ├── AtmWithdrawl.cs
        ├── CreateRecurringPayment.cs
        ├── CustomerCheckingAccount.cs
        └── Transfer.cs
```

---

## 🧠 Orleans Concepts — Deep Dive

### 1. Grains — The Heart of Everything

**A grain is an object that represents an entity in your system.** Each grain has:
- A **unique identity** (its key — Guid, long, or string)
- **Private state** (data that only it can modify)
- **Behavior** (methods that operate on its state)
- **Single-threaded execution** (processes one message at a time)

**In this project:**
- Every checking account is an `ICheckingAccountGrain` identified by a GUID
- Every ATM is an `IAtmGrain` identified by a GUID
- Every customer is an `ICustomerGrain` identified by a GUID

**Code pattern:**
```csharp
// 1. Define the INTERFACE (in Abstractions/)
public interface ICheckingAccountGrain : IGrainWithGuidKey
{
    Task<decimal> GetBalance();
    Task Debit(decimal amount);
}

// 2. Implement the GRAIN (in Grains/)
public class CheckingAccountGrain : Grain, ICheckingAccountGrain
{
    public async Task<decimal> GetBalance() { ... }
    public async Task Debit(decimal amount) { ... }
}

// 3. Call from CLIENT
var grain = clusterClient.GetGrain<ICheckingAccountGrain>(accountId);
decimal balance = await grain.GetBalance();
```

**Key types for grain identity:**

| Interface | Key Type | When to Use |
|-----------|----------|-------------|
| `IGrainWithGuidKey` | `Guid` | Entities with GUID IDs (accounts, ATMs, customers) |
| `IGrainWithIntegerKey` | `long` | Entities with numeric IDs (counters, lookup tables) |
| `IGrainWithStringKey` | `string` | Entities with string IDs (usernames, email addresses) |

### 2. Virtual Actor Model

In Orleans, grains are **virtual** — they always exist logically.

```csharp
// This ALWAYS works, even if the account doesn't exist yet
var grain = clusterClient.GetGrain<ICheckingAccountGrain>(Guid.NewGuid());

// If the grain didn't exist before, Orleans creates it now
await grain.Initialise(1000.00);
```

**Lifecycle (automatic):**

1. **Activation:** First call to a grain → Orleans creates an instance in memory on some silo
2. **Execution:** The grain processes requests
3. **Idle:** After a period of inactivity, Orleans deactivates the grain (removes from memory)
4. **Reactivation:** Next call → Orleans creates a new instance, reloads state from storage

This is all **transparent** — your code never manages lifecycle.

### 3. Silo & Client — Two Roles

| Role | What It Does | In This Project |
|------|-------------|-----------------|
| **Silo** | Hosts grains, executes grain methods, persists state | `Orleans.Silo` (console app) |
| **Client** | Sends requests to grains via the cluster | `Orleans.Client` (web API) |

```csharp
// In Silo/Program.cs — Configure what the silo does
Host.CreateDefaultBuilder(args)
    .UseOrleans(siloBuilder =>
    {
        siloBuilder.UseAzureStorageClustering(...);
        siloBuilder.AddAzureBlobGrainStorage("blobStorage", ...);
    });

// In Client/Program.cs — Configure how the client connects
builder.Host.UseOrleansClient((context, client) =>
{
    client.UseAzureStorageClustering(...);  // Must match silo
    client.Configure<ClusterOptions>(options =>
    {
        options.ClusterId = "practiceCluster";  // Must match silo
        options.ServiceId = "practiceService";  // Must match silo
    });
});
```

**Critical:** The ClusterId and ServiceId **must match** between Client and Silo. Otherwise, the client won't find the silo!

### 4. Clustering — How Silos Find Each Other

When a silo starts, it registers itself in Azure Table Storage. When a client connects, it reads the same table to find available silos.

```csharp
siloBuilder.UseAzureStorageClustering(options =>
{
    options.TableServiceClient = new TableServiceClient("UseDevelopmentStorage=true;");
});
```

**Other clustering options:** `UseAdoNetClustering` (SQL Server), `UseConsulClustering`, `UseDynamoDBClustering`, `UseLocalhostClustering` (for development).

### 5. Grain State Persistence

Orleans needs to save grain state somewhere. This project demonstrates **two types**:

#### a) Non-Transactional Persistence (`IPersistentState<T>`)

```csharp
// In grain constructor (DI)
[PersistentState("checkingAccount", "blobStorage")]
IPersistentState<CheckingAccountState> checkingAccountState

// Reading (in-memory, no network call)
var accountType = _checkingAccountState.State.AccountType;

// Writing (persists to storage)
_checkingAccountState.State.AccountType = "Premium";
await _checkingAccountState.WriteStateAsync();

// Deleting
await _checkingAccountState.ClearStateAsync();
```

**When to use:** Metadata, configuration, data that doesn't need ACID guarantees.

#### b) Transactional Persistence (`ITransactionalState<T>`)

```csharp
// In grain constructor (DI)
[TransactionalState("balance")]
ITransactionalState<BalanceState> balanceState

// Reading (safe, consistent read)
var balance = await _balanceState.PerformRead(state => state.Balance);

// Updating (within a transaction)
await _balanceState.PerformUpdate(state =>
{
    state.Balance = state.Balance + amount;
});
```

**When to use:** Financial data, inventory counts — anything needing ACID guarantees.

#### Storage Providers in This Project

| Provider Name | Backend | Used By |
|--------------|---------|---------|
| `"tableStorage"` | Azure Table Storage | CustomerGrain, StatlessTransferProcessingGrain |
| `"blobStorage"` | Azure Blob Storage | CheckingAccountGrain (metadata) |
| `"PubSubStore"` | Azure Table Storage | Stream subscription state |
| `(default transactional)` | Azure Table Storage | CheckingAccountGrain (balance), AtmGrain |

### 6. Transactions — ACID Across Grains

Orleans provides **distributed ACID transactions** that can span multiple grains. This is a unique feature — most actor frameworks don't support this!

```csharp
// CLIENT-SIDE transaction (in Program.cs)
await transactionClient.RunTransaction(TransactionOption.Create, async () =>
{
    // ATM balance changes
    var atmGrain = clusterClient.GetGrain<IAtmGrain>(atmId);
    await atmGrain.Withdraw(accountId, 50.00);

    // Account gets debited — same transaction!
    var accountGrain = clusterClient.GetGrain<ICheckingAccountGrain>(accountId);
    await accountGrain.Debit(50.00);
});
// If either fails, BOTH are rolled back
```

**Transaction attributes on grain methods:**

| Attribute | Behavior |
|-----------|----------|
| `[Transaction(TransactionOption.Create)]` | Must start a NEW transaction |
| `[Transaction(TransactionOption.CreateOrJoin)]` | Join existing transaction or create new |
| `[Transaction(TransactionOption.Suppress)]` | Don't participate in transactions |
| (no attribute) | Non-transactional |

**Rules:**
- `[Transaction(TransactionOption.Create)]` methods **cannot** be called inside another transaction — they must be the root
- `[Transaction(TransactionOption.CreateOrJoin)]` methods can be called independently OR nested inside another transaction
- Transactions can span grains on **different silos** — Orleans handles distributed coordination via the **Transaction Manager**

### 7. Streams — Event-Driven Communication

**Orleans Streams** provide a pub-sub (publish/subscribe) messaging system. They allow grains to communicate asynchronously through events.

**Real-world analogy:** Like subscribing to a newsletter. The publisher writes content, the postal service delivers it, and subscribers receive it.

```csharp
// PUBLISHER (CheckingAccountGrain)
var streamProvider = this.GetStreamProvider("StreamProvider");
var streamId = StreamId.Create("BalanceStream", this.GetGrainId().GetGuidKey());
var stream = streamProvider.GetStream<BalanceChangeEvent>(streamId);
await stream.OnNextAsync(new BalanceChangeEvent
{
    CheckingAccountId = accountId,
    Balance = newBalance
});

// SUBSCRIBER (CustomerGrain)
var stream = streamProvider.GetStream<BalanceChangeEvent>(streamId);
await stream.SubscribeAsync(this);  // this = IAsyncObserver<BalanceChangeEvent>

// Handle received events
public async Task OnNextAsync(BalanceChangeEvent item, StreamSequenceToken? token = null)
{
    // Update cached balance
    _customerState.State.CheckingAccountBalanceById[item.CheckingAccountId] = item.Balance;
}
```

**Stream configurations in this project:**
- **Provider:** `"StreamProvider"` (Azure Queue-based)
- **Namespace:** `"BalanceStream"` (categories events by type)
- **Key:** The checking account's GUID (each account has its own stream)
- **Events:** `BalanceChangeEvent` — contains account ID and new balance

**Why streams are better than polling:**

| Polling (bad) | Streams (good) |
|---------------|----------------|
| Call every account to get balance | Receive events when balance changes |
| Network calls for every check | No network calls for passive updates |
| Wastes CPU and bandwidth | Only processes actual changes |
| May see stale data | Real-time updates |

### 8. Reminders — Persistent Timers

**Reminders** are like cron jobs for grains. They fire at scheduled intervals, survive silo restarts, and reactivate grains if needed.

```csharp
// Register a reminder (fires every N minutes, first after N minutes)
await this.RegisterOrUpdateReminder(
    "RecurringPayment:::paymentGuid",           // Unique name
    TimeSpan.FromMinutes(intervalMinutes),      // When to first fire
    TimeSpan.FromMinutes(intervalMinutes)        // Repeat interval
);

// Handle reminder firing (implements IRemindable)
public async Task ReceiveReminder(string reminderName, TickStatus status)
{
    if (reminderName.StartsWith("RecurringPayment"))
    {
        // Find the payment config and process it
        await _transactionClient.RunTransaction(TransactionOption.Create, async () =>
        {
            await Debit(paymentAmount);
        });
    }
}
```

**Important:** The `ReceiveReminder` method is called **outside any transaction**, so you must create a new transaction if you need one (as shown above).

**Reminders vs Timers:**

| Feature | Reminders | Timers (`RegisterTimer`) |
|---------|-----------|--------------------------|
| **Persistence** | ✅ Survive silo restarts | ❌ Lost on restart |
| **Deactivation** | ✅ Fire even when grain is idle | ❌ Timer stops if grain deactivated |
| **Guarantee** | At-least-once delivery | Best-effort |
| **Storage** | Azure Table / SQL Server | In-memory |
| **Use case** | Scheduled payments, maintenance | Periodic in-memory operations |

### 9. Stateless Workers

Normally, each grain key has only **one activation** in the cluster. `[StatelessWorker]` changes this — Orleans can create **multiple activations** on any silo.

```csharp
[StatelessWorker]
public class StatlessTransferProcessingGrain : Grain, IStatlessTransferProcessingGrain
{
    public async Task ProcessTransfer(Guid fromId, Guid toId, decimal amount)
    {
        // This can run in PARALLEL with other transfers
        // Multiple activations handle different requests simultaneously
    }
}
```

**When to use:**
- Request routing / orchestration
- Stateless computation
- Tasks that can be parallelized
- When you DON'T need single-instance guarantees

**Caveat:** State in StatelessWorker grains is **per-activation**. The TransferCount in this project is approximate — each activation has its own counter!

### 10. Grain Call Filters

Filters intercept every grain call, similar to ASP.NET Core middleware.

**Incoming Filter (runs on Silo, for every request arriving at grains):**
```csharp
// Registered in Silo/Program.cs
siloBuilder.AddIncomingGrainCallFilter<LoggingIncomingGrainCallFilter>();

// Implementation
public async Task Invoke(IIncomingGrainCallContext context)
{
    // BEFORE the grain method runs
    _logger.LogInformation($"Calling {context.MethodName} on {context.Grain}");

    // Let the grain method execute
    await context.Invoke();

    // AFTER the grain method completes
    // Could log response time, etc.
}
```

**Outgoing Filter (runs on Client, for every request leaving the client):**
```csharp
// Registered in Client/Program.cs
client.AddOutgoingGrainCallFilter<LoggingOutgoingGrainCallFilter>();
```

**Grain-Level Filter (runs only for a specific grain type):**
```csharp
// AtmGrain implements IIncomingGrainCallFilter directly
public class AtmGrain : Grain, IAtmGrain, IIncomingGrainCallFilter
{
    public async Task Invoke(IIncomingGrainCallContext context)
    {
        _logger.LogInformation($"ATM: {context.MethodName}");
        await context.Invoke();
    }
}
```

### 11. One-Way Calls (Fire-and-Forget)

The `[OneWay]` attribute makes a grain method **fire-and-forget**. The caller gets back an immediately-completed Task and never waits for the grain to finish.

```csharp
// Interface
[OneWay]
Task FireAndForgetWork();

// Implementation (takes 2 seconds and THROWS!)
public async Task FireAndForgetWork()
{
    await Task.Delay(2000);
    throw new NotSupportedException("Caller never sees this!");
}

// Caller — returns IMMEDIATELY, doesn't wait
await grain.FireAndForgetWork();
Console.WriteLine("This runs before the grain finishes!");
```

**Important:** The caller **never knows** if the one-way call succeeded or failed. The exception thrown in `FireAndForgetWork` is logged on the silo but never propagated to the client.

### 12. Grain Cancellation Tokens

Like standard `CancellationToken` but works **across the network**:

```csharp
// Client
var cts = new GrainCancellationTokenSource();
var task = grain.CancelableWork(cts.Token, 15);  // 15-second work

// Cancel after 5 seconds
await Task.Delay(5000);
await cts.Cancel();

// Grain
public async Task CancelableWork(GrainCancellationToken token, long seconds)
{
    try
    {
        await Task.Delay(TimeSpan.FromSeconds(seconds), token.CancellationToken);
    }
    catch (TaskCanceledException)
    {
        // Gracefully handle cancellation
    }
}
```

### 13. Reentrant Grains

By default, grains process **one message at a time**. `[Reentrant]` allows limited interleaving — if grain A calls grain B, and grain B calls back to grain A, the callback is allowed to execute even though grain A is still busy.

```csharp
[Reentrant]
public class CheckingAccountGrain : Grain, ICheckingAccountGrain
```

**Without [Reentrant], this scenario DEADLOCKS:**
1. Grain A calls method on Grain B
2. Grain B calls method on Grain A
3. Grain A is waiting for B's response, so it can't process B's call → DEADLOCK

`[Reentrant]` allows step 3 to proceed — grain A processes B's call while still waiting for its original call to B.

**Use [Reentrant] only when:** You're sure that interleaving calls won't corrupt grain state.

---

## 🌐 API Reference — All 13 Endpoints

All endpoints are served from `http://localhost:5297`.

### Checking Account Operations

| Method | Endpoint | Description | Request Body | Response |
|--------|----------|-------------|-------------|----------|
| `POST` | `/checkingaccount` | Create a new account | `{ "openingBalance": 1000.00 }` | `201 Created` |
| `GET` | `/checkingaccount/{id}/balance` | Get account balance | — | `200 OK` → decimal |
| `POST` | `/checkingaccount/{id}/debit` | Withdraw money | `{ "amount": 50.00 }` | `204 No Content` |
| `POST` | `/checkingaccount/{id}/credit` | Deposit money | `{ "amount": 100.00 }` | `204 No Content` |
| `POST` | `/checkingaccount/{id}/recurringPayment` | Set up auto-payment | `{ "paymentId": "guid", "paymentAmount": 9.99, "paymentRecurrsEveryMinutes": 43200 }` | `204 No Content` |
| `POST` | `/checkingaccount/{id}/fireandforgetwork` | Test fire-and-forget | — | `204 No Content` |
| `POST` | `/checkingaccount/{id}/cancellablework` | Test cancellation | — | `204 No Content` |

### ATM Operations

| Method | Endpoint | Description | Request Body | Response |
|--------|----------|-------------|-------------|----------|
| `POST` | `/atm` | Create an ATM | `{ "initialAtmCashBalance": 10000.00 }` | `201 Created` |
| `GET` | `/atm/{id}/balance` | Get ATM cash balance | — | `200 OK` → decimal |
| `POST` | `/atm/{id}/withdrawl` | Withdraw from ATM | `{ "checkingAccountId": "guid", "amount": 50.00 }` | `204 No Content` |

### Customer Operations

| Method | Endpoint | Description | Request Body | Response |
|--------|----------|-------------|-------------|----------|
| `GET` | `/customer/{id}/networth` | Get customer net worth | — | `200 OK` → decimal |
| `POST` | `/customer/{id}/addcheckingaccount` | Link account to customer | `{ "accountId": "guid" }` | `204 No Content` |

### Transfer Operations

| Method | Endpoint | Description | Request Body | Response |
|--------|----------|-------------|-------------|----------|
| `POST` | `/transfer` | Transfer between accounts | `{ "fromAccountId": "guid", "toAccountId": "guid", "amount": 100.00 }` | `204 No Content` |

---

## 🧪 Testing the API

### Full End-to-End Test Script

```bash
# Terminal 1: Start Azurite
azurite --silent

# Terminal 2: Start the Silo
cd Orleans.Silo
dotnet run

# Terminal 3: Start the Client
cd Orleans.Client
dotnet run

# Terminal 4: Test the API
```

```bash
# ============================================
# 1. Create a checking account
# ============================================
curl -s -X POST http://localhost:5297/checkingaccount \
  -H "Content-Type: application/json" \
  -d '{"openingBalance": 1000.00}' \
  -w "\n%{redirect_url}\n"
# Save the returned GUID for subsequent calls
# The Location header contains: checkingaccount/{guid}
SET ACCOUNT_ID="your-guid-here"

# ============================================
# 2. Check balance (should be 1000.00)
# ============================================
curl -s http://localhost:5297/checkingaccount/%ACCOUNT_ID%/balance

# ============================================
# 3. Deposit money
# ============================================
curl -s -X POST http://localhost:5297/checkingaccount/%ACCOUNT_ID%/credit \
  -H "Content-Type: application/json" \
  -d '{"amount": 500.00}'

# ============================================
# 4. Withdraw money
# ============================================
curl -s -X POST http://localhost:5297/checkingaccount/%ACCOUNT_ID%/debit \
  -H "Content-Type: application/json" \
  -d '{"amount": 200.00}'

# ============================================
# 5. Check balance (should be 1300.00)
# ============================================
curl -s http://localhost:5297/checkingaccount/%ACCOUNT_ID%/balance

# ============================================
# 6. Set up a recurring payment (every 1 minute)
# ============================================
curl -s -X POST http://localhost:5297/checkingaccount/%ACCOUNT_ID%/recurringPayment \
  -H "Content-Type: application/json" \
  -d '{"paymentId": "11111111-1111-1111-1111-111111111111", "paymentAmount": 9.99, "paymentRecurrsEveryMinutes": 1}'

# ============================================
# 7. Create an ATM
# ============================================
curl -s -X POST http://localhost:5297/atm \
  -H "Content-Type: application/json" \
  -d '{"initialAtmCashBalance": 10000.00}' \
  -w "\n%{redirect_url}\n"
SET ATM_ID="your-atm-guid-here"

# ============================================
# 8. Withdraw from ATM (uses multi-grain transaction!)
# ============================================
curl -s -X POST http://localhost:5297/atm/%ATM_ID%/withdrawl \
  -H "Content-Type: application/json" \
  -d "{\"checkingAccountId\": \"%ACCOUNT_ID%\", \"amount\": 60.00}"

# ============================================
# 9. Check both balances after ATM withdrawal
# ============================================
curl -s http://localhost:5297/checkingaccount/%ACCOUNT_ID%/balance
curl -s http://localhost:5297/atm/%ATM_ID%/balance

# ============================================
# 10. Create a customer and link the account
# ============================================
SET CUSTOMER_ID="aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"
curl -s -X POST http://localhost:5297/customer/%CUSTOMER_ID%/addcheckingaccount \
  -H "Content-Type: application/json" \
  -d "{\"accountId\": \"%ACCOUNT_ID%\"}"

# ============================================
# 11. Get customer net worth
# ============================================
curl -s http://localhost:5297/customer/%CUSTOMER_ID%/networth

# ============================================
# 12. Create a second account and transfer money
# ============================================
curl -s -X POST http://localhost:5297/checkingaccount \
  -H "Content-Type: application/json" \
  -d '{"openingBalance": 500.00}' \
  -w "\n%{redirect_url}\n"
SET ACCOUNT2_ID="your-second-guid-here"

curl -s -X POST http://localhost:5297/transfer \
  -H "Content-Type: application/json" \
  -d "{\"fromAccountId\": \"%ACCOUNT_ID%\", \"toAccountId\": \"%ACCOUNT2_ID%\", \"amount\": 100.00}"

# ============================================
# 13. Check both balances (verify transfer)
# ============================================
curl -s http://localhost:5297/checkingaccount/%ACCOUNT_ID%/balance
curl -s http://localhost:5297/checkingaccount/%ACCOUNT2_ID%/balance
```

---

## 🔧 Common Issues & Troubleshooting

### "Can't find the cluster"

```
Orleans.Runtime.ClusterConfigurationException: Cannot connect to cluster
```

**Solution:** Make sure:
1. Azurite is running
2. The Silo is running and connected
3. The Client's `ClusterId`/`ServiceId` match the Silo's

### "Table/Queue doesn't exist"

Azurite creates storage on-demand. If you see errors about missing tables/queues, restart Azurite and the Silo.

### "Port already in use"

Azurite uses ports 10000 (blob), 10001 (queue), 10002 (table). If something else uses these ports, either stop that service or configure different ports.

### "The remote endpoint was not in a required state"

This usually means the Silo isn't running or crashed. Check the Silo's console output.

### Silo crashed with "Azure.Storage" errors

Azurite isn't running. Start it with `azurite --silent` before starting the Silo.

### Reminders not firing

Check that the reminder service is configured:
```csharp
siloBuilder.UseAzureTableReminderService(...);
```
Also check that your grain implements `IRemindable` and has the `ReceiveReminder` method.

### Transactions not working

Make sure you've called both:
```csharp
// In the Silo
siloBuilder.UseTransactions();
siloBuilder.AddAzureTableTransactionalStateStorageAsDefault(...);

// In the Client
client.UseTransactions();
```

### Stream messages not received

1. Check that the stream provider is configured (`AddAzureQueueStreams`)
2. Check that `PubSubStore` is configured
3. Ensure the subscriber grain implements `IAsyncObserver<T>`
4. Ensure `OnActivateAsync` calls `GetAllSubscriptionHandles()` and `ResumeAsync(this)`

---

## 📖 Going Further

### Recommended Learning Path

| Step | Topic | What to Read |
|------|-------|-------------|
| 1 | Orleans Fundamentals | [Microsoft Orleans Overview](https://learn.microsoft.com/en-us/dotnet/orleans/overview) |
| 2 | Grain Lifecycle | [Grain Lifecycle](https://learn.microsoft.com/en-us/dotnet/orleans/grains/grain-lifecycle) |
| 3 | Grain Persistence | [Grain Persistence](https://learn.microsoft.com/en-us/dotnet/orleans/grains/grain-persistence) |
| 4 | Transactions | [Orleans Transactions](https://learn.microsoft.com/en-us/dotnet/orleans/grains/transactions) |
| 5 | Streams | [Orleans Streams](https://learn.microsoft.com/en-us/dotnet/orleans/streaming) |
| 6 | Reminders & Timers | [Timers and Reminders](https://learn.microsoft.com/en-us/dotnet/orleans/grains/timers-and-reminders) |
| 7 | Deployment | [Deploy to Azure](https://learn.microsoft.com/en-us/dotnet/orleans/deployment) |

### Exercises to Try

1. **Add overdraft protection** — Check that balance >= amount before debiting
2. **Add transaction history** — Store a list of transactions in grain state
3. **Add interest calculation** — Use a reminder to calculate monthly interest
4. **Add a new grain type** — Create `ILoanGrain` for loans
5. **Deploy to Azure** — Replace Azurite with real Azure Storage and deploy to Azure Container Apps
6. **Add authentication** — Use grain call filters to check authorization
7. **Multi-silo deployment** — Start two silo instances and observe load balancing

---

## 🏆 What You've Learned

After working through this project, you understand:

| Concept | You Know... |
|---------|------------|
| **Grains** | How to define interfaces & implementations, different key types |
| **Virtual Actors** | Automatic activation/deactivation lifecycle |
| **Silos** | How servers host grains in a cluster |
| **Clustering** | How silos discover each other |
| **Persistence** | How grain state is saved & loaded |
| **Transactions** | How ACID works across multiple grains |
| **Streams** | How event-driven pub-sub works in Orleans |
| **Reminders** | How persistent timers trigger grain methods |
| **Stateless Workers** | When and how to use multiple activations |
| **Filters** | How to intercept grain calls |
| **OneWay** | Fire-and-forget messaging |
| **Cancellation** | How to cancel long-running grain operations |
| **Reentrant** | Safe call-back interleaving |

---

> **Built with ❤️ to help .NET developers learn distributed systems with Microsoft Orleans.**
>
> *"Distributed systems should be accessible to every developer, not just infrastructure engineers."*
