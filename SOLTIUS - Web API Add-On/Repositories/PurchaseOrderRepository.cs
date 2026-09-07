using Dapper;
using SOLTIUS_Web_API_Add_On.Database.Interfaces;
using SOLTIUS_Web_API_Add_On.Models.Configuration;
using SOLTIUS_Web_API_Add_On.Models.Transaction;
using SOLTIUS_Web_API_Add_On.Services.Configuration;
using System.Data.Common;

namespace SOLTIUS_Web_API_Add_On.Repositories
{
    public class PurchaseOrderRepository : IPurchaseOrderRepository
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly IConfigurationService _configurationService;

        public PurchaseOrderRepository(IDatabaseConnectionFactory connectionFactory, IConfigurationService configurationService)
        {
            _connectionFactory = connectionFactory;
            _configurationService = configurationService;
        }

        public async Task<StagingPoStatus> GetStatusByWebTxNumberAsync(string webTxNumber)
        {
            DBConfig config = _configurationService.GetDatabaseConfig();
            bool isMySql = config.DBType == DatabaseType.MySql;

            using DbConnection conn = _connectionFactory.CreateConnection(config);
            await conn.OpenAsync();

            string query = isMySql
                ? @"SELECT SOL_ID as Id, SOL_WEB_TX_NUMBER as WebTxNumber, SOL_PROCESS_STATUS as ProcessStatus, 
                           SOL_DOCENTRY as DocEntry, SOL_ERRORMESSAGE as ErrorMessage, SOL_PROCESSED_AT as ProcessedAt, 
                           SOL_CREATED_AT as CreatedAt 
                    FROM SOL_PURCHASE_ORDER_HEADER 
                    WHERE SOL_WEB_TX_NUMBER = @WebTxNumber 
                    ORDER BY SOL_ID DESC LIMIT 1;"
                : @"SELECT TOP 1 SOL_ID as Id, SOL_WEB_TX_NUMBER as WebTxNumber, SOL_PROCESS_STATUS as ProcessStatus, 
                           SOL_DOCENTRY as DocEntry, SOL_ERRORMESSAGE as ErrorMessage, SOL_PROCESSED_AT as ProcessedAt, 
                           SOL_CREATED_AT as CreatedAt 
                    FROM SOL_PURCHASE_ORDER_HEADER 
                    WHERE SOL_WEB_TX_NUMBER = @WebTxNumber 
                    ORDER BY SOL_ID DESC;";

            return await conn.QueryFirstOrDefaultAsync<StagingPoStatus>(query, new { WebTxNumber = webTxNumber });
        }

        public async Task<IEnumerable<StagingPoStatus>> GetSyncStatusListAsync(DateTime? since)
        {
            DBConfig config = _configurationService.GetDatabaseConfig();
            bool isMySql = config.DBType == DatabaseType.MySql;

            using DbConnection conn = _connectionFactory.CreateConnection(config);
            await conn.OpenAsync();

            string query = isMySql
                ? @"SELECT SOL_ID as Id, SOL_WEB_TX_NUMBER as WebTxNumber, SOL_PROCESS_STATUS as ProcessStatus, 
                           SOL_DOCENTRY as DocEntry, SOL_ERRORMESSAGE as ErrorMessage, SOL_PROCESSED_AT as ProcessedAt, 
                           SOL_CREATED_AT as CreatedAt 
                    FROM SOL_PURCHASE_ORDER_HEADER 
                    WHERE (@Since IS NULL OR COALESCE(SOL_UPDATED_AT, SOL_CREATED_AT) >= @Since)
                    ORDER BY COALESCE(SOL_UPDATED_AT, SOL_CREATED_AT) ASC;"
                : @"SELECT SOL_ID as Id, SOL_WEB_TX_NUMBER as WebTxNumber, SOL_PROCESS_STATUS as ProcessStatus, 
                           SOL_DOCENTRY as DocEntry, SOL_ERRORMESSAGE as ErrorMessage, SOL_PROCESSED_AT as ProcessedAt, 
                           SOL_CREATED_AT as CreatedAt 
                    FROM SOL_PURCHASE_ORDER_HEADER 
                    WHERE (@Since IS NULL OR COALESCE(SOL_UPDATED_AT, SOL_CREATED_AT) >= @Since)
                    ORDER BY COALESCE(SOL_UPDATED_AT, SOL_CREATED_AT) ASC;";

            return await conn.QueryAsync<StagingPoStatus>(query, new { Since = since });
        }

        public async Task InsertPurchaseOrderAsync(PurchaseOrderHeader purchaseOrder)
        {
            // Idempotent duplicate check: If already exists with same WebTxNumber
            if (!string.IsNullOrWhiteSpace(purchaseOrder.WebTxNumber))
            {
                var existing = await GetStatusByWebTxNumberAsync(purchaseOrder.WebTxNumber);
                if (existing != null)
                {
                    if (existing.ProcessStatus == 1 || !string.IsNullOrWhiteSpace(existing.DocEntry))
                    {
                        throw new InvalidOperationException($"Dokumen sudah ter posting ke SAP dengan nomor SAP {existing.DocEntry}");
                    }
                    else if (existing.ProcessStatus == 0)
                    {
                        throw new InvalidOperationException($"Dokumen {purchaseOrder.WebTxNumber} sudah ada di antrean staging Hub dan sedang menunggu proses ke SAP.");
                    }
                }
            }

            DBConfig config = _configurationService.GetDatabaseConfig();
            bool isMySql = config.DBType == DatabaseType.MySql;

            using DbConnection conn = _connectionFactory.CreateConnection(config);
            await conn.OpenAsync();
            using var transaction = await conn.BeginTransactionAsync();

            try
            {
                // Ekstraksi UDF dan fallback tanggal
                string headerUdfJson = purchaseOrder.GetUdfJson();
                DateTime docDate = purchaseOrder.DocDate == DateTime.MinValue ? DateTime.Now : purchaseOrder.DocDate;
                DateTime docDueDate = purchaseOrder.DocDueDate == DateTime.MinValue ? docDate.AddDays(7) : purchaseOrder.DocDueDate;
                DateTime taxDate = purchaseOrder.TaxDate == DateTime.MinValue ? docDate : purchaseOrder.TaxDate;

                // --- Header (menggunakan tabel & field berawalan SOL_) ---
                string headerSql = isMySql
                    ? @"INSERT INTO SOL_PURCHASE_ORDER_HEADER
                        (SOL_CARDCODE, SOL_CARDNAME, SOL_DOCDATE, SOL_DOCDUEDATE, SOL_TAXDATE, SOL_REMARKS, 
                         SOL_WEB_TX_NUMBER, SOL_WEB_TX_ID, SOL_UDF_DATA, SOL_PROCESS_STATUS, SOL_CREATED_AT)
                        VALUES (@CardCode, @CardName, @DocDate, @DocDueDate, @TaxDate, @Remarks, 
                         @WebTxNumber, @WebTxId, @UdfData, 0, NOW());
                        SELECT LAST_INSERT_ID();"
                    : @"INSERT INTO SOL_PURCHASE_ORDER_HEADER
                        (SOL_CARDCODE, SOL_CARDNAME, SOL_DOCDATE, SOL_DOCDUEDATE, SOL_TAXDATE, SOL_REMARKS, 
                         SOL_WEB_TX_NUMBER, SOL_WEB_TX_ID, SOL_UDF_DATA, SOL_PROCESS_STATUS, SOL_CREATED_AT)
                        VALUES (@CardCode, @CardName, @DocDate, @DocDueDate, @TaxDate, @Remarks, 
                         @WebTxNumber, @WebTxId, @UdfData, 0, GETDATE());
                        SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

                long headerId = await conn.ExecuteScalarAsync<long>(headerSql, new
                {
                    CardCode = purchaseOrder.CardCode,
                    CardName = purchaseOrder.CardName,
                    DocDate = docDate,
                    DocDueDate = docDueDate,
                    TaxDate = taxDate,
                    Remarks = purchaseOrder.Remarks,
                    WebTxNumber = purchaseOrder.WebTxNumber,
                    WebTxId = purchaseOrder.WebTxId,
                    UdfData = string.IsNullOrEmpty(headerUdfJson) ? null : headerUdfJson
                }, transaction);

                // --- Details (menggunakan tabel & field berawalan SOL_) ---
                int lineNum = 0;
                foreach (var line in purchaseOrder.DocumentLines)
                {
                    string lineUdfJson = line.GetUdfJson();

                    string detailSql = isMySql
                        ? @"INSERT INTO SOL_PURCHASE_ORDER_DETAIL
                            (SOL_HEADER_ID, SOL_LINENUM, SOL_ITEMCODE, SOL_ITEMNAME, SOL_WAREHOUSE, 
                             SOL_QUANTITY, SOL_PRICE, SOL_VAT_GROUP, SOL_WEB_LINE_ID, SOL_UDF_DATA, SOL_PROCESS_STATUS, SOL_CREATED_AT)
                            VALUES (@HeaderId, @LineNum, @ItemCode, @ItemName, @Warehouse, 
                             @Quantity, @Price, @VatGroup, @WebLineId, @UdfData, 0, NOW());"
                        : @"INSERT INTO SOL_PURCHASE_ORDER_DETAIL
                            (SOL_HEADER_ID, SOL_LINENUM, SOL_ITEMCODE, SOL_ITEMNAME, SOL_WAREHOUSE, 
                             SOL_QUANTITY, SOL_PRICE, SOL_VAT_GROUP, SOL_WEB_LINE_ID, SOL_UDF_DATA, SOL_PROCESS_STATUS, SOL_CREATED_AT)
                            VALUES (@HeaderId, @LineNum, @ItemCode, @ItemName, @Warehouse, 
                             @Quantity, @Price, @VatGroup, @WebLineId, @UdfData, 0, GETDATE());";

                    await conn.ExecuteAsync(detailSql, new
                    {
                        HeaderId = headerId,
                        LineNum = ++lineNum,
                        ItemCode = line.ItemCode,
                        ItemName = line.ItemDescription,
                        Warehouse = line.WarehouseCode,
                        Quantity = line.Quantity,
                        Price = line.Price,
                        VatGroup = line.VatGroup,
                        WebLineId = line.WebLineId,
                        UdfData = string.IsNullOrEmpty(lineUdfJson) ? null : lineUdfJson
                    }, transaction);
                }

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
public async Task<bool> DeleteFromStagingAsync(string webTxNumber)
        {
            DBConfig config = _configurationService.GetDatabaseConfig();
            bool isMySql = config.DBType == DatabaseType.MySql;

            using DbConnection conn = _connectionFactory.CreateConnection(config);
            await conn.OpenAsync();

            string detailSql = isMySql
                ? @"DELETE d FROM SOL_PURCHASE_ORDER_DETAIL d
                    INNER JOIN SOL_PURCHASE_ORDER_HEADER h ON d.SOL_HEADER_ID = h.SOL_ID
                    WHERE h.SOL_WEB_TX_NUMBER = @WebTxNumber;"
                : @"DELETE d FROM SOL_PURCHASE_ORDER_DETAIL d
                    INNER JOIN SOL_PURCHASE_ORDER_HEADER h ON d.SOL_HEADER_ID = h.SOL_ID
                    WHERE h.SOL_WEB_TX_NUMBER = @WebTxNumber;";

            string headerSql = "DELETE FROM SOL_PURCHASE_ORDER_HEADER WHERE SOL_WEB_TX_NUMBER = @WebTxNumber;";

            await conn.ExecuteAsync(detailSql, new { WebTxNumber = webTxNumber });
            int affected = await conn.ExecuteAsync(headerSql, new { WebTxNumber = webTxNumber });
            return affected > 0;
        }

        public async Task<bool> RequestCancelInSapAsync(string webTxNumber, string sapDocNum, string reason)
        {
            DBConfig config = _configurationService.GetDatabaseConfig();
            bool isMySql = config.DBType == DatabaseType.MySql;

            using DbConnection conn = _connectionFactory.CreateConnection(config);
            await conn.OpenAsync();

            string sql = isMySql
                ? @"UPDATE SOL_PURCHASE_ORDER_HEADER
                    SET SOL_PROCESS_STATUS = 3,
                        SOL_ERRORMESSAGE = @Reason,
                        SOL_PROCESSED_AT = NOW()
                    WHERE SOL_WEB_TX_NUMBER = @WebTxNumber;"
                : @"UPDATE SOL_PURCHASE_ORDER_HEADER
                    SET SOL_PROCESS_STATUS = 3,
                        SOL_ERRORMESSAGE = @Reason,
                        SOL_PROCESSED_AT = GETDATE()
                    WHERE SOL_WEB_TX_NUMBER = @WebTxNumber;";

            int affected = await conn.ExecuteAsync(sql, new { WebTxNumber = webTxNumber, Reason = "CANCEL_REQUEST: " + (reason ?? "Pembatalan via Web App") });
            return affected > 0;
        }
    }
}
