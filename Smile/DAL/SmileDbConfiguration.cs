using System;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.SqlServer;
using System.Runtime.Remoting.Messaging;

namespace Smile.DAL
{
    /// <summary>
    /// EF 設定：連到 Azure SQL 時，遇到暫時性錯誤自動重試。
    /// 免費的 Azure SQL 閒置一段時間會自動暫停，第一個連線要等它醒來，這時重試可以避免使用者直接看到錯誤頁。
    /// 本機 LocalDB 不受影響。
    /// </summary>
    public class SmileDbConfiguration : DbConfiguration
    {
        private static readonly bool IsAzureSql =
            (ConfigurationManager.ConnectionStrings["SmileContext"] != null ? ConfigurationManager.ConnectionStrings["SmileContext"].ConnectionString : "")
            .IndexOf("database.windows.net", StringComparison.OrdinalIgnoreCase) >= 0;

        public SmileDbConfiguration()
        {
            SetExecutionStrategy("System.Data.SqlClient", () =>
                IsAzureSql && !SuspendExecutionStrategy
                    ? (IDbExecutionStrategy)new SqlAzureExecutionStrategy(6, TimeSpan.FromSeconds(10))
                    : new DefaultExecutionStrategy());
        }

        /// <summary>建表、匯入示範資料時暫停重試機制（重試機制不支援 EF 移轉自己開的交易）</summary>
        public static bool SuspendExecutionStrategy
        {
            get { return (bool?)CallContext.LogicalGetData("SuspendExecutionStrategy") ?? false; }
            set { CallContext.LogicalSetData("SuspendExecutionStrategy", value); }
        }
    }
}
