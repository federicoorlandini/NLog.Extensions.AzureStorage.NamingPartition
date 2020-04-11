using Microsoft.Extensions.Caching.Memory;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;


namespace NLog.Extensions.AzureTableStorage.Partition
{
    class CloudTableCache
    {
        private const int CacheSlidingExpirationInMinutes = 10;

        private readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
        private readonly ConcurrentDictionary<object, SemaphoreSlim> _locks = new ConcurrentDictionary<object, SemaphoreSlim>();
        private readonly CloudTableClient _cloudTableClient;

        public CloudTableCache(string azureStorageConnectionString)
        {
            // Define which client to use to write in the Azure Table
            _cloudTableClient = CloudStorageAccount.Parse(azureStorageConnectionString).CreateCloudTableClient();
        }

        public async Task<CloudTable> GetOrSet(string tableNamePrefix)
        {
            if (!_cache.TryGetValue(tableNamePrefix, out CloudTable cacheEntry))
            {
                // Check if the specific semaphore is green
                SemaphoreSlim mylock = _locks.GetOrAdd(tableNamePrefix, k => new SemaphoreSlim(1, 1));

                await mylock.WaitAsync();

                try
                {
                    if (!_cache.TryGetValue(tableNamePrefix, out cacheEntry))
                    {
                        // Key not in cache, so get data and create the entry
                        cacheEntry = CreateCloudTable(tableNamePrefix);
                        _cache.Set(tableNamePrefix, cacheEntry, TimeSpan.FromMinutes(CacheSlidingExpirationInMinutes));
                    }
                }
                finally
                {
                    mylock.Release();
                }
            }

            return cacheEntry;
        }

        private CloudTable CreateCloudTable(string tableNamePrefix)
        {
            var tableName = BuildTableName(tableNamePrefix);
            var table = _cloudTableClient.GetTableReference(tableName);

            table.CreateIfNotExistsAsync();

            return table;
        }

        private string BuildTableName(string tableNamePrefix)
        {
            return tableNamePrefix + DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString("D2");
        }
    }
}
