﻿using System.Linq;
using System.Collections.Generic;

using Couchbase;
using Hangfire.Server;
using Hangfire.Storage;
using Hangfire.Logging;
using Newtonsoft.Json;
using Couchbase.Core.Serialization;

using Hangfire.Couchbase.Queue;
using Hangfire.Couchbase.Documents.Json;

namespace Hangfire.Couchbase
{
    /// <summary>
    /// DocumentDbStorage extend the storage option for Hangfire.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    public sealed class CouchbaseStorage : JobStorage
    {
        internal CouchbaseStorageOptions Options { get; }

        internal PersistentJobQueueProviderCollection QueueProviders { get; }

        internal Cluster Client { get; }

        /// <summary>
        /// Initializes the DocumentDbStorage form the url auth secret provide.
        /// </summary>
        /// <param name="configurationSectionName">The configuration section name</param>
        /// <param name="bucket">The name of the database to connect with</param>
        /// <param name="options">The DocumentDbStorageOptions object to override any of the options</param>
        public CouchbaseStorage(string configurationSectionName, string bucket = "default", CouchbaseStorageOptions options = null)
        {
            Options = options ?? new CouchbaseStorageOptions();
            Options.Bucket = bucket;

            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                ContractResolver = new DocumentContractResolver()
            };

            Client = new Cluster(configurationSectionName);
            Client.Configuration.Serializer = () => new DefaultSerializer(settings, settings);

            JobQueueProvider provider = new JobQueueProvider(this);
            QueueProviders = new PersistentJobQueueProviderCollection(provider);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override IStorageConnection GetConnection() => new CouchbaseConnection(this);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override IMonitoringApi GetMonitoringApi() => new DocumentDbMonitoringApi(this);

#pragma warning disable 618
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<IServerComponent> GetComponents()
#pragma warning restore 618
        {
            yield return new ExpirationManager(this);
            yield return new CountersAggregator(this);
        }

        /// <summary>
        /// Prints out the storage options
        /// </summary>
        /// <param name="logger"></param>
        public override void WriteOptionsToLog(ILog logger)
        {
            logger.Info("Using the following options for Azure DocumentDB job storage:");
            logger.Info($"     Couchbase Url: {string.Join(",", Client.Configuration.Servers.Select(s => s.AbsoluteUri))}");
            logger.Info($"     Request Timeout: {Options.RequestTimeout}");
            logger.Info($"     Counter Agggerate Interval: {Options.CountersAggregateInterval.TotalSeconds} seconds");
            logger.Info($"     Queue Poll Interval: {Options.QueuePollInterval.TotalSeconds} seconds");
            logger.Info($"     Expiration Check Interval: {Options.ExpirationCheckInterval.TotalSeconds} seconds");
            logger.Info($"     Queue: {string.Join(",", Options.Queues)}");
        }

        /// <summary>
        /// Return the name of the database
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"Bucket : {Options.Bucket}";
    }
}
