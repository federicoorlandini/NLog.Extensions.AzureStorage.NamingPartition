using Microsoft.WindowsAzure.Storage.Table;
using NLog.Config;
using NLog.Targets;
using System;

namespace NLog.Extensions.AzureStorage.NamingPartition
{
    [Target("AzureTableStorageByMonth")]
    public class AzureTableStorageByMonthTarget : TargetWithLayout
    {
        [RequiredParameter]
        public string ConnectionString { get; set; }

        [RequiredParameter]
        public string TableNamePrefix { get; set; }

        public string LogTimeStampFormat { get; set; } = "O";

        private CloudTableCache _cloudTableCache;

        private readonly string _machineName = Environment.MachineName;

        protected override void InitializeTarget()
        {
            ValidateTableNamePrefix();

            _cloudTableCache = new CloudTableCache(ConnectionString);

            base.InitializeTarget();
        }

        private void ValidateTableNamePrefix()
        {
            // There are limitations in the name for an Azure Storage Table
            // (see https://blogs.msdn.microsoft.com/jmstall/2014/06/12/azure-storage-naming-rules/)
            // In particular, the name cannot be longer than 63 characters, so we need to check if the
            // prefix name for the table is no longer than 63 chars - 6 chars (year and month) = 57
            if(TableNamePrefix.Length > 57)
            {
                throw new InvalidOperationException("The TableNamePrefix property cannot be longer than 57 characters.");
            }
        }

        protected override void Write(LogEventInfo logEvent)
        {
            
            var table = _cloudTableCache.GetOrSet(TableNamePrefix).Result;

            var layoutMessage = RenderLogEvent(Layout, logEvent);
            var entity = new NLogEntity(logEvent, layoutMessage, _machineName, logEvent.LoggerName, LogTimeStampFormat);
            var insertOperation = TableOperation.Insert(entity);

            table.Execute(insertOperation);

            base.Write(logEvent);
        }
    }
}
