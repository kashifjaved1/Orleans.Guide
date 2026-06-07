// ==========================================================================
// Orleans.Silo/Program.cs - THE SILO (SERVER) ENTRY POINT
// ==========================================================================
// 
// WHAT IS A SILO?
// ---------------------------------------------------------------
// In Orleans, a SILO is a server process that hosts and executes grains.
// Think of it as the "worker" or "node" in your distributed system.
// 
// Each silo:
// - Registers itself in the cluster (so clients can find it)
// - Hosts grain instances in memory
// - Executes grain methods when called
// - Persists grain state to storage
// - Communicates with other silos in the cluster
//
// You can run ONE silo (for development) or HUNDREDS (for production).
// Orleans handles the distribution automatically.
//
// AZURE STORAGE EMULATOR:
// ---------------------------------------------------------------
// This project uses "UseDevelopmentStorage=true;" connection string
// which connects to the Azurite (Azure Storage Emulator) running locally.
// Azure Storage provides: Tables, Blobs, Queues
//
// To run this project, you need Azurite installed:
//   npm install -g azurite
//   azurite --silent
//
// Or use Docker:
//   docker run -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite
// ==========================================================================

using Azure.Storage.Queues;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Orleans.Grains.Filters;

// ==========================================================================
// HOST BUILDER PATTERN (same as ASP.NET Core)
// ==========================================================================
// Host.CreateDefaultBuilder sets up:
// - App configuration (appsettings.json)
// - Logging (console, debug)
// - Dependency Injection container
// - Environment-based configuration
//
// .UseOrleans() adds Orleans to the host, configuring the silo.
// ==========================================================================
await Host.CreateDefaultBuilder(args)
    .UseOrleans(siloBuilder =>
    {
        // ======================================================================
        // 1. CLUSTERING - How silos find each other
        // ======================================================================
        // UseAzureStorageClustering: Silos register themselves in an Azure Table.
        // When a new silo starts, it adds its entry to the table.
        // When a client connects, it reads the table to find available silos.
        //
        // Alternatives: Consul, ZooKeeper, SQL Server, or local/dev clustering.
        // For production, you'd use a real Azure Storage account, not the emulator.
        // ======================================================================
        siloBuilder.UseAzureStorageClustering(configureOptions: options =>
        {
            options.TableServiceClient = new Azure.Data.Tables.TableServiceClient("UseDevelopmentStorage=true;");
        });

        // ======================================================================
        // 2. CLUSTER IDENTITY
        // ======================================================================
        // ClusterId: Distinguishes between different Orleans clusters.
        //   If you run multiple clusters (e.g., dev, staging, production),
        //   they should have different ClusterIds so they don't interfere.
        //
        // ServiceId: Identifies the application service.
        //   Used by Orleans for service-level features like reminders and
        //   stream persistence. Should be the SAME across all environments
        //   for the same application.
        // ======================================================================
        siloBuilder.Configure<ClusterOptions>(options =>
        {
            options.ClusterId = "practiceCluster";
            options.ServiceId = "practiceService";
        });

        // ======================================================================
        // 3. GRAIN STATE PERSISTENCE STORAGE PROVIDERS
        // ======================================================================
        // Orleans needs somewhere to save grain state. We configure multiple
        // storage providers with different NAMES, then grains reference them
        // by name via [PersistentState("stateName", "providerName")].

        // "tableStorage": Azure Table Storage - used by CustomerGrain,
        // StatlessTransferProcessingGrain for their persistent state.
        // Tables are good for structured, queryable data.
        siloBuilder.AddAzureTableGrainStorage("tableStorage", configureOptions: options =>
        {
            options.TableServiceClient = new Azure.Data.Tables.TableServiceClient("UseDevelopmentStorage=true;");
        });

        // "blobStorage": Azure Blob Storage - used by CheckingAccountGrain
        // for its metadata state. Blob storage stores the ENTIRE object
        // as a single JSON document - great for complex nested objects.
        siloBuilder.AddAzureBlobGrainStorage("blobStorage", configureOptions: options =>
        {
            options.BlobServiceClient = new Azure.Storage.Blobs.BlobServiceClient("UseDevelopmentStorage=true;");
        });

        // "PubSubStore": Special store for stream subscription information.
        // Orleans uses this to remember which grains are subscribed to which
        // streams. This is configured separately below with the stream setup.

        // ======================================================================
        // 4. REMINDERS SERVICE
        // ======================================================================
        // Reminders are persistent timers that survive silo restarts.
        // They're stored in Azure Tables. When a reminder fires, Orleans
        // reactivates the grain (if needed) and calls ReceiveReminder().
        //
        // Used by: CheckingAccountGrain for recurring payments.
        // ======================================================================
        siloBuilder.UseAzureTableReminderService(configureOptions: options =>
        {
            options.Configure(o => o.TableServiceClient = new Azure.Data.Tables.TableServiceClient("UseDevelopmentStorage=true;"));
        });

        // ======================================================================
        // 5. TRANSACTIONAL STATE STORAGE
        // ======================================================================
        // This configures the DEFAULT storage for ITransactionalState<T>.
        // When a grain has [TransactionalState("name")] ITransactionalState<T>,
        // Orleans uses this storage to persist the transactional state.
        //
        // Used by: CheckingAccountGrain (balance), AtmGrain (cash inventory).
        // ======================================================================
        siloBuilder.AddAzureTableTransactionalStateStorageAsDefault(configureOptions: options =>
        {
            options.TableServiceClient = new Azure.Data.Tables.TableServiceClient("UseDevelopmentStorage=true;");
        });

        // ======================================================================
        // 6. ENABLE TRANSACTIONS
        // ======================================================================
        // This MUST be called to enable Orleans' distributed ACID transactions.
        // Without this, [Transaction] attributes on grain methods are ignored.
        // ======================================================================
        siloBuilder.UseTransactions();

        // ======================================================================
        // 7. STREAMS (Azure Queue-based)
        // ======================================================================
        // Orleans Streams provide a pub-sub messaging system between grains.
        // 
        // Azure Queue Streams: Events are published to Azure Storage Queues.
        // "StreamProvider" is the name used to reference this provider in code.
        //
        // PubSubStore: Where Orleans stores stream subscription information.
        // This is separate from the stream events themselves.
        //
        // Used by: CheckingAccountGrain (publishes balance changes),
        // CustomerGrain (subscribes to balance changes).
        // ======================================================================
        siloBuilder.AddAzureQueueStreams("StreamProvider", optionsBuilder =>
        {
            optionsBuilder.Configure(options => { options.QueueServiceClient = new QueueServiceClient("UseDevelopmentStorage=true;"); });
        })
        .AddAzureTableGrainStorage("PubSubStore", configureOptions: options =>
        {
            options.Configure(o => o.TableServiceClient = new Azure.Data.Tables.TableServiceClient("UseDevelopmentStorage=true;"));
        });

        // ======================================================================
        // 8. INCOMING GRAIN CALL FILTER
        // ======================================================================
        // Registers a silo-wide filter that intercepts ALL incoming grain calls.
        // Every method call to every grain will pass through this filter.
        //
        // See: Filters/LoggingIncomingGrainCallFilter.cs
        // ======================================================================
        siloBuilder.AddIncomingGrainCallFilter<LoggingIncomingGrainCallFilter>();

        // ======================================================================
        // (OPTIONAL) GRAIN COLLECTION / DEACTIVATION SETTINGS
        // ======================================================================
        // By default, Orleans deactivates idle grains after some time.
        // Uncomment and adjust these if you want grains to stay in memory longer:
        //
        //siloBuilder.Configure<GrainCollectionOptions>(options =>
        //{
        //    options.CollectionQuantum = TimeSpan.FromSeconds(20);
        //    options.CollectionAge = TimeSpan.FromSeconds(20);
        //});
        // ======================================================================

    }).RunConsoleAsync();
