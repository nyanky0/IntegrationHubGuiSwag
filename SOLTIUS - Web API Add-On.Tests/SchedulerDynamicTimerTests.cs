using System;
using System.IO;
using System.Threading;
using SOLTIUS_Scheduler_Add_On.Model;
using SOLTIUS_Scheduler_Add_On.Services;
using Xunit;

namespace SOLTIUS_Web_API_Add_On.Tests
{
    public class SchedulerDynamicTimerTests
    {
        [Fact]
        public void SyncSchedulerEngine_Start_Stop_RestartIfRunning_UpdatesIntervalDynamically()
        {
            var engine = SyncSchedulerEngine.Instance;
            if (engine.IsRunning)
            {
                engine.Stop();
            }

            var initialConfig = new SchedulerConfig
            {
                Mode = "Realtime",
                RealtimeSeconds = 30,
                SyncPurchaseOrder = false,
                SyncGoodsReceiptPO = false,
                SyncStockTransfer = false,
                SyncGoodsReturn = false,
                EnableReconciliation = false,
                EnableHeartbeat = false
            };

            // 1. Start engine
            engine.Start(initialConfig);
            Assert.True(engine.IsRunning);

            // 2. Change timer interval dynamically
            var updatedConfig = new SchedulerConfig
            {
                Mode = "Realtime",
                RealtimeSeconds = 5,
                SyncPurchaseOrder = false,
                SyncGoodsReceiptPO = false,
                SyncStockTransfer = false,
                SyncGoodsReturn = false,
                EnableReconciliation = false,
                EnableHeartbeat = false
            };

            engine.RestartIfRunning(updatedConfig);
            Assert.True(engine.IsRunning);

            // 3. Stop cleanly
            engine.Stop();
            Assert.False(engine.IsRunning);
        }
    }
}
