using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using NLog.Config;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        private readonly string _machineName = "localhost";

        protected override void InitializeTarget()
        {
            // Define which client to use to write in the Azure Table
            _cloudTableCache = new CloudTableCache(ConnectionString);

            base.InitializeTarget();
        }

        protected override void Write(LogEventInfo logEvent)
        {
            
            var table = _cloudTableCache.GetOrSet(TableNamePrefix).Result;

            var layoutMessage = RenderLogEvent(Layout, logEvent);
            var entity = new NLogEntity(logEvent, layoutMessage, _machineName, logEvent.LoggerName, LogTimeStampFormat);
            var insertOperation = TableOperation.Insert(entity);

            table.Execute(insertOperation);

            // Write in the Azure Table
            base.Write(logEvent);
        }
    }
}
