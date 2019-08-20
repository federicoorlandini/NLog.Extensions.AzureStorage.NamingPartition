using Microsoft.Extensions.Caching.Memory;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace NLog.Extensions.AzureStorage.NamingPartition
{
    class CloudTableCache
    {
        private const int CacheSlidingExpirationInMinutes = 10;

        private MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
        private ConcurrentDictionary<object, SemaphoreSlim> _locks = new ConcurrentDictionary<object, SemaphoreSlim>();

        private readonly CloudTableClient _cloudTableClient;

        public CloudTableCache(string azureStorageConnectionString)
        {
            // Define which client to use to write in the Azure Table
            _cloudTableClient = CloudStorageAccount.Parse(azureStorageConnectionString).CreateCloudTableClient();
        }

        public async Task<CloudTable> GetOrSet(string tableNamePrefix)
        {
            CloudTable cacheEntry;
            if (!_cache.TryGetValue(tableNamePrefix, out cacheEntry))// Look for cache key.
            {
                SemaphoreSlim mylock = _locks.GetOrAdd(tableNamePrefix, k => new SemaphoreSlim(1, 1));

                await mylock.WaitAsync();

                try
                {
                    if (!_cache.TryGetValue(tableNamePrefix, out cacheEntry))
                    {
                        // Key not in cache, so get data.
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
