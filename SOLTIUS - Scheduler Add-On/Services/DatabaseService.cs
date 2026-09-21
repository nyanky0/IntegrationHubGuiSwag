using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using SOLTIUS_Scheduler_Add_On.Model;

namespace SOLTIUS_Scheduler_Add_On.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;
        private readonly int _maxRetryCount;

        public DatabaseService(string connectionString, int maxRetryCount = 5)
        {
            _connectionString = connectionString;
            _maxRetryCount = maxRetryCount;
        }

        public void InitializeTables()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string query = @"
                                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='TBL_SYNC_HISTORY' and xtype='U')
                                    CREATE TABLE TBL_SYNC_HISTORY (
                                        LogID INT IDENTITY(1,1) PRIMARY KEY,
                                        DocType VARCHAR(50),
                                        DocEntry VARCHAR(50),
                                        CardCode VARCHAR(100),
                                        ItemCode VARCHAR(100),
                                        Quantity DECIMAL(18,2),
                                        Price DECIMAL(18,2),
                                        WarehouseCode VARCHAR(50),
                                        Status VARCHAR(50),
                                        ErrorSource VARCHAR(100),
                                        ErrorMessage VARCHAR(MAX),
                                        CreatedAt DATETIME
                                    );

                                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='TBL_SYNC_ERROR' and xtype='U')
                                    CREATE TABLE TBL_SYNC_ERROR (
                                        ErrorID INT IDENTITY(1,1) PRIMARY KEY,
                                        DocType VARCHAR(50),
                                        DocEntry VARCHAR(50),
                                        CardCode VARCHAR(100),
                                        ItemCode VARCHAR(100),
                                        Quantity DECIMAL(18,2),
                                        Price DECIMAL(18,2),
                                        WarehouseCode VARCHAR(50),
                                        ErrorMessage VARCHAR(MAX),
                                        IsResolved BIT DEFAULT 0,
                                        CreatedAt DATETIME
                                    );";

                using (var command = new SqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public void SaveLogToDatabase(SyncLogModel log)
        {
            if (string.IsNullOrEmpty(_connectionString)) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // 1. Insert to TBL_SYNC_HISTORY (all logs)
                    string queryHistory = @"INSERT INTO TBL_SYNC_HISTORY
                                          (DocType, DocEntry, CardCode, ItemCode, Quantity, Price, WarehouseCode, Status, ErrorSource, ErrorMessage, CreatedAt)
                                          VALUES (@DocType, @DocEntry, @CardCode, @ItemCode, @Qty, @Price, @Whs, @Status, @ErrSrc, @ErrMsg, @CreatedAt)";

                    using (SqlCommand cmd = new SqlCommand(queryHistory, conn))
                    {
                        cmd.Parameters.AddWithValue("@DocType", log.DocType ?? "");
                        cmd.Parameters.AddWithValue("@DocEntry", log.DocEntry ?? "");
                        cmd.Parameters.AddWithValue("@CardCode", log.CardCode ?? "");
                        cmd.Parameters.AddWithValue("@ItemCode", log.ItemCode ?? "");
                        cmd.Parameters.AddWithValue("@Qty", log.Quantity);
                        cmd.Parameters.AddWithValue("@Price", log.Price);
                        cmd.Parameters.AddWithValue("@Whs", log.WarehouseCode ?? "");
                        cmd.Parameters.AddWithValue("@Status", log.Status ?? "");
                        cmd.Parameters.AddWithValue("@ErrSrc", log.ErrorSource ?? "");
                        cmd.Parameters.AddWithValue("@ErrMsg", log.ErrorMessage ?? "");
                        cmd.Parameters.AddWithValue("@CreatedAt", log.CreatedAt == DateTime.MinValue ? DateTime.Now : log.CreatedAt);
                        cmd.ExecuteNonQuery();
                    }

                    // 2. If Failed, also insert to TBL_SYNC_ERROR
                    if (log.Status == "Failed")
                    {
                        string queryError = @"INSERT INTO TBL_SYNC_ERROR
                                              (DocType, DocEntry, CardCode, ItemCode, Quantity, Price, WarehouseCode, ErrorMessage, IsResolved, CreatedAt)
                                              VALUES (@DocType, @DocEntry, @CardCode, @ItemCode, @Qty, @Price, @Whs, @ErrMsg, 0, @CreatedAt)";

                        using (SqlCommand cmdErr = new SqlCommand(queryError, conn))
                        {
                            cmdErr.Parameters.AddWithValue("@DocType", log.DocType ?? "");
                            cmdErr.Parameters.AddWithValue("@DocEntry", log.DocEntry ?? "");
                            cmdErr.Parameters.AddWithValue("@CardCode", log.CardCode ?? "");
                            cmdErr.Parameters.AddWithValue("@ItemCode", log.ItemCode ?? "");
                            cmdErr.Parameters.AddWithValue("@Qty", log.Quantity);
                            cmdErr.Parameters.AddWithValue("@Price", log.Price);
                            cmdErr.Parameters.AddWithValue("@Whs", log.WarehouseCode ?? "");
                            cmdErr.Parameters.AddWithValue("@ErrMsg", log.ErrorMessage ?? "");
                            cmdErr.Parameters.AddWithValue("@CreatedAt", log.CreatedAt == DateTime.MinValue ? DateTime.Now : log.CreatedAt);
                            cmdErr.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("DB Insert Error: " + ex.Message);
            }
        }

        public void MarkErrorsAsResolved(string cardCode, string itemCode)
        {
            if (string.IsNullOrEmpty(_connectionString)) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string queryUpdate = @"UPDATE TBL_SYNC_ERROR
                                           SET IsResolved = 1
                                           WHERE CardCode = @CardCode
                                           AND ItemCode = @ItemCode
                                           AND IsResolved = 0";

                    using (SqlCommand cmd = new SqlCommand(queryUpdate, conn))
                    {
                        cmd.Parameters.AddWithValue("@CardCode", cardCode);
                        cmd.Parameters.AddWithValue("@ItemCode", itemCode);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("DB Update Error: " + ex.Message);
            }
        }

        public List<SyncLogModel> LoadLogHistory()
        {
            var loadedList = new List<SyncLogModel>();
            if (string.IsNullOrEmpty(_connectionString)) return loadedList;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"SELECT TOP 500 DocType, DocEntry, CardCode, ItemCode, Quantity, Price,
                                            WarehouseCode, Status, ErrorSource, ErrorMessage, CreatedAt
                                     FROM TBL_SYNC_HISTORY ORDER BY LogID DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            loadedList.Add(new SyncLogModel
                            {
                                DocType = reader["DocType"]?.ToString() ?? "",
                                DocEntry = reader["DocEntry"]?.ToString() ?? "",
                                CardCode = reader["CardCode"]?.ToString() ?? "",
                                ItemCode = reader["ItemCode"]?.ToString() ?? "",
                                Quantity = Convert.IsDBNull(reader["Quantity"]) ? 0 : Convert.ToDouble(reader["Quantity"]),
                                Price = Convert.IsDBNull(reader["Price"]) ? 0.0 : Convert.ToDouble(reader["Price"]),
                                WarehouseCode = reader["WarehouseCode"]?.ToString() ?? "",
                                Status = reader["Status"]?.ToString() ?? "",
                                ErrorSource = reader["ErrorSource"]?.ToString() ?? "",
                                ErrorMessage = reader["ErrorMessage"]?.ToString() ?? "",
                                CreatedAt = Convert.IsDBNull(reader["CreatedAt"]) ? DateTime.MinValue : Convert.ToDateTime(reader["CreatedAt"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("LoadLog Error: " + ex.Message);
            }
            return loadedList;
        }

        /// <summary>
        /// Mengambil Goods Receipt PO pending dari tabel staging (SOL_PROCESS_STATUS = 0).
        /// </summary>
        public List<PendingPurchaseOrder> LoadPendingGoodsReceiptPOs()
        {
            var result = new List<PendingPurchaseOrder>();
            if (string.IsNullOrEmpty(_connectionString)) return result;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string query = @"
                        SELECT h.SOL_ID AS HeaderId, h.SOL_CARDCODE AS CardCode, h.SOL_CARDNAME AS CardName,
                               h.SOL_DOCDATE AS DocDate, h.SOL_DOCDUEDATE AS DocDueDate, h.SOL_TAXDATE AS TaxDate,
                               h.SOL_REMARKS AS Remarks, h.SOL_WEB_TX_NUMBER AS WebTxNumber, h.SOL_WEB_TX_ID AS WebTxId,
                               h.SOL_UDF_DATA AS HeaderUdfData,
                               d.SOL_LINENUM AS LineNum, d.SOL_ITEMCODE AS ItemCode, d.SOL_ITEMNAME AS ItemName,
                               d.SOL_WAREHOUSE AS Warehouse, d.SOL_QUANTITY AS Quantity, d.SOL_PRICE AS Price,
                               d.SOL_VAT_GROUP AS VatGroup, d.SOL_WEB_LINE_ID AS WebLineId, d.SOL_UDF_DATA AS LineUdfData
                        FROM SOL_GRPO_HEADER h
                        INNER JOIN SOL_GRPO_DETAIL d ON d.SOL_HEADER_ID = h.SOL_ID
                        WHERE (h.SOL_PROCESS_STATUS = 0 
                               OR (h.SOL_PROCESS_STATUS = 2 AND h.SOL_RETRYCOUNT < 3 AND (
                                   (h.SOL_RETRYCOUNT = 1 AND DATEDIFF(second, ISNULL(h.SOL_UPDATED_AT, h.SOL_CREATED_AT), GETDATE()) >= 30)
                                   OR (h.SOL_RETRYCOUNT = 2 AND DATEDIFF(second, ISNULL(h.SOL_UPDATED_AT, h.SOL_CREATED_AT), GETDATE()) >= 120)
                               )))
                        ORDER BY h.SOL_ID, d.SOL_LINENUM";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        PendingPurchaseOrder current = null;
                        long currentHeaderId = -1;

                        while (reader.Read())
                        {
                            long headerId = Convert.ToInt64(reader["HeaderId"]);

                            if (current == null || headerId != currentHeaderId)
                            {
                                current = new PendingPurchaseOrder
                                {
                                    HeaderId = headerId,
                                    CardCode = reader["CardCode"]?.ToString() ?? "",
                                    CardName = reader["CardName"]?.ToString() ?? "",
                                    DocDate = Convert.IsDBNull(reader["DocDate"]) ? DateTime.Now : Convert.ToDateTime(reader["DocDate"]),
                                    DocDueDate = Convert.IsDBNull(reader["DocDueDate"]) ? DateTime.Now.AddDays(7) : Convert.ToDateTime(reader["DocDueDate"]),
                                    TaxDate = Convert.IsDBNull(reader["TaxDate"]) ? DateTime.Now : Convert.ToDateTime(reader["TaxDate"]),
                                    Remarks = reader["Remarks"]?.ToString() ?? "",
                                    WebTxNumber = Convert.IsDBNull(reader["WebTxNumber"]) ? null : reader["WebTxNumber"].ToString(),
                                    WebTxId = Convert.IsDBNull(reader["WebTxId"]) ? (long?)null : Convert.ToInt64(reader["WebTxId"]),
                                    UdfDataJson = Convert.IsDBNull(reader["HeaderUdfData"]) ? null : reader["HeaderUdfData"].ToString()
                                };
                                result.Add(current);
                                currentHeaderId = headerId;
                            }

                            current.Lines.Add(new PendingPurchaseOrderLine
                            {
                                LineNum = Convert.IsDBNull(reader["LineNum"]) ? 0 : Convert.ToInt32(reader["LineNum"]),
                                ItemCode = reader["ItemCode"]?.ToString() ?? "",
                                ItemName = reader["ItemName"]?.ToString() ?? "",
                                Warehouse = reader["Warehouse"]?.ToString() ?? "",
                                Quantity = Convert.IsDBNull(reader["Quantity"]) ? 0 : Convert.ToDecimal(reader["Quantity"]),
                                Price = Convert.IsDBNull(reader["Price"]) ? 0 : Convert.ToDecimal(reader["Price"]),
                                VatGroup = Convert.IsDBNull(reader["VatGroup"]) ? null : reader["VatGroup"].ToString(),
                                WebLineId = Convert.IsDBNull(reader["WebLineId"]) ? (long?)null : Convert.ToInt64(reader["WebLineId"]),
                                UdfDataJson = Convert.IsDBNull(reader["LineUdfData"]) ? null : reader["LineUdfData"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Gagal memuat Goods Receipt PO pending dari staging.", ex);
            }

            return result;
        }

        public void UpdateGoodsReceiptPOStatus(long headerId, int processStatus, string errorMessage = null, string docEntry = null)
        {
            if (string.IsNullOrEmpty(_connectionString)) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string query = @"
                        UPDATE SOL_GRPO_HEADER
                        SET SOL_PROCESS_STATUS = CASE 
                                WHEN @Status = 1 THEN 1 
                                WHEN @Status = 2 AND (SOL_RETRYCOUNT + 1) >= @MaxRetry THEN 3 
                                ELSE @Status 
                            END,
                            SOL_ERRORMESSAGE = CASE 
                                WHEN @Status = 2 AND (SOL_RETRYCOUNT + 1) >= @MaxRetry THEN '[DEAD-LETTER] ' + ISNULL(@ErrMsg, 'Max retry limit reached')
                                ELSE @ErrMsg 
                            END,
                            SOL_DOCENTRY = CASE WHEN @DocEntry IS NOT NULL THEN @DocEntry ELSE SOL_DOCENTRY END,
                            SOL_RETRYCOUNT = CASE WHEN @Status = 1 THEN 0 ELSE SOL_RETRYCOUNT + 1 END,
                            SOL_PROCESSED_AT = GETDATE(),
                            SOL_UPDATED_AT = GETDATE()
                        WHERE SOL_ID = @HeaderId;

                        UPDATE SOL_GRPO_DETAIL
                        SET SOL_PROCESS_STATUS = CASE 
                                WHEN @Status = 1 THEN 1 
                                WHEN @Status = 2 AND (SELECT SOL_RETRYCOUNT FROM SOL_GRPO_HEADER WHERE SOL_ID = @HeaderId) >= @MaxRetry THEN 3 
                                ELSE @Status 
                            END
                        WHERE SOL_HEADER_ID = @HeaderId;";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@HeaderId", headerId);
                        cmd.Parameters.AddWithValue("@Status", processStatus);
                        cmd.Parameters.AddWithValue("@MaxRetry", _maxRetryCount > 0 ? _maxRetryCount : 3);
                        cmd.Parameters.AddWithValue("@ErrMsg", (object)errorMessage ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DocEntry", (object)docEntry ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("DB Update GRPO Status Error: " + ex.Message);
            }
        }

        public bool IsGoodsReceiptPORetryLimitExceeded(long headerId)
        {
            if (string.IsNullOrEmpty(_connectionString)) return false;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"SELECT SOL_RETRYCOUNT FROM SOL_GRPO_HEADER WHERE SOL_ID = @HeaderId";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@HeaderId", headerId);
                        object result = cmd.ExecuteScalar();
                        if (result == null || result == DBNull.Value) return false;
                        return Convert.ToInt32(result) >= _maxRetryCount;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        public void MarkGoodsReceiptPOAsExceededRetryLimit(long headerId)
        {
            if (string.IsNullOrEmpty(_connectionString)) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"
                        UPDATE SOL_GRPO_HEADER
                        SET SOL_PROCESS_STATUS = 3,
                            SOL_ERRORMESSAGE = 'Max retry limit exceeded',
                            SOL_UPDATED_AT = GETDATE()
                        WHERE SOL_ID = @HeaderId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@HeaderId", headerId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("DB MarkGoodsReceiptPOExceeded Error: " + ex.Message);
            }
        }

        /// <summary>
        /// Mengambil Stock Transfer pending dari tabel staging (SOL_PROCESS_STATUS = 0).
        /// </summary>
        public List<PendingPurchaseOrder> LoadPendingStockTransfers()
        {
            var result = new List<PendingPurchaseOrder>();
            if (string.IsNullOrEmpty(_connectionString)) return result;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string query = @"
                        SELECT h.SOL_ID AS HeaderId, h.SOL_CARDCODE AS CardCode, h.SOL_CARDNAME AS CardName,
                               h.SOL_DOCDATE AS DocDate, h.SOL_DOCDUEDATE AS DocDueDate, h.SOL_TAXDATE AS TaxDate,
                               h.SOL_REMARKS AS Remarks, h.SOL_WEB_TX_NUMBER AS WebTxNumber, h.SOL_WEB_TX_ID AS WebTxId,
                               h.SOL_UDF_DATA AS HeaderUdfData,
                               d.SOL_LINENUM AS LineNum, d.SOL_ITEMCODE AS ItemCode, d.SOL_ITEMNAME AS ItemName,
                               d.SOL_FROM_WAREHOUSE AS FromWarehouse, d.SOL_WAREHOUSE AS Warehouse,
                               d.SOL_QUANTITY AS Quantity, d.SOL_PRICE AS Price,
                               d.SOL_WEB_LINE_ID AS WebLineId, d.SOL_UDF_DATA AS LineUdfData
                        FROM SOL_STOCK_TRANSFER_HEADER h
                        INNER JOIN SOL_STOCK_TRANSFER_DETAIL d ON d.SOL_HEADER_ID = h.SOL_ID
                        WHERE (h.SOL_PROCESS_STATUS = 0 
                               OR (h.SOL_PROCESS_STATUS = 2 AND h.SOL_RETRYCOUNT < 3 AND (
                                   (h.SOL_RETRYCOUNT = 1 AND DATEDIFF(second, ISNULL(h.SOL_UPDATED_AT, h.SOL_CREATED_AT), GETDATE()) >= 30)
                                   OR (h.SOL_RETRYCOUNT = 2 AND DATEDIFF(second, ISNULL(h.SOL_UPDATED_AT, h.SOL_CREATED_AT), GETDATE()) >= 120)
                               )))
                        ORDER BY h.SOL_ID, d.SOL_LINENUM";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        PendingPurchaseOrder current = null;
                        long currentHeaderId = -1;

                        while (reader.Read())
                        {
                            long headerId = Convert.ToInt64(reader["HeaderId"]);

                            if (current == null || headerId != currentHeaderId)
                            {
                                current = new PendingPurchaseOrder
                                {
                                    HeaderId = headerId,
                                    CardCode = reader["CardCode"]?.ToString() ?? "",
                                    CardName = reader["CardName"]?.ToString() ?? "",
                                    DocDate = Convert.IsDBNull(reader["DocDate"]) ? DateTime.Now : Convert.ToDateTime(reader["DocDate"]),
                                    DocDueDate = Convert.IsDBNull(reader["DocDueDate"]) ? DateTime.Now.AddDays(7) : Convert.ToDateTime(reader["DocDueDate"]),
                                    TaxDate = Convert.IsDBNull(reader["TaxDate"]) ? DateTime.Now : Convert.ToDateTime(reader["TaxDate"]),
                                    Remarks = reader["Remarks"]?.ToString() ?? "",
                                    WebTxNumber = Convert.IsDBNull(reader["WebTxNumber"]) ? null : reader["WebTxNumber"].ToString(),
                                    WebTxId = Convert.IsDBNull(reader["WebTxId"]) ? (long?)null : Convert.ToInt64(reader["WebTxId"]),
                                    UdfDataJson = Convert.IsDBNull(reader["HeaderUdfData"]) ? null : reader["HeaderUdfData"].ToString()
                                };
                                result.Add(current);
                                currentHeaderId = headerId;
                            }

                            current.Lines.Add(new PendingPurchaseOrderLine
                            {
                                LineNum = Convert.IsDBNull(reader["LineNum"]) ? 0 : Convert.ToInt32(reader["LineNum"]),
                                ItemCode = reader["ItemCode"]?.ToString() ?? "",
                                ItemName = reader["ItemName"]?.ToString() ?? "",
                                FromWarehouse = reader["FromWarehouse"]?.ToString() ?? "",
                                Warehouse = reader["Warehouse"]?.ToString() ?? "",
                                Quantity = Convert.IsDBNull(reader["Quantity"]) ? 0 : Convert.ToDecimal(reader["Quantity"]),
                                Price = Convert.IsDBNull(reader["Price"]) ? 0 : Convert.ToDecimal(reader["Price"]),
                                WebLineId = Convert.IsDBNull(reader["WebLineId"]) ? (long?)null : Convert.ToInt64(reader["WebLineId"]),
                                UdfDataJson = Convert.IsDBNull(reader["LineUdfData"]) ? null : reader["LineUdfData"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Gagal memuat Stock Transfer pending dari staging.", ex);
            }

            return result;
        }

        public void UpdateStockTransferStatus(long headerId, int processStatus, string errorMessage = null, string docEntry = null)
        {
            if (string.IsNullOrEmpty(_connectionString)) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string query = @"
                        UPDATE SOL_STOCK_TRANSFER_HEADER
                        SET SOL_PROCESS_STATUS = CASE 
                                WHEN @Status = 1 THEN 1 
                                WHEN @Status = 2 AND (SOL_RETRYCOUNT + 1) >= @MaxRetry THEN 3 
                                ELSE @Status 
                            END,
                            SOL_ERRORMESSAGE = CASE 
                                WHEN @Status = 2 AND (SOL_RETRYCOUNT + 1) >= @MaxRetry THEN '[DEAD-LETTER] ' + ISNULL(@ErrMsg, 'Max retry limit reached')
                                ELSE @ErrMsg 
                            END,
                            SOL_DOCENTRY = CASE WHEN @DocEntry IS NOT NULL THEN @DocEntry ELSE SOL_DOCENTRY END,
                            SOL_RETRYCOUNT = CASE WHEN @Status = 1 THEN 0 ELSE SOL_RETRYCOUNT + 1 END,
                            SOL_PROCESSED_AT = GETDATE(),
                            SOL_UPDATED_AT = GETDATE()
                        WHERE SOL_ID = @HeaderId;

                        UPDATE SOL_STOCK_TRANSFER_DETAIL
                        SET SOL_PROCESS_STATUS = CASE 
                                WHEN @Status = 1 THEN 1 
                                WHEN @Status = 2 AND (SELECT SOL_RETRYCOUNT FROM SOL_STOCK_TRANSFER_HEADER WHERE SOL_ID = @HeaderId) >= @MaxRetry THEN 3 
                                ELSE @Status 
                            END
                        WHERE SOL_HEADER_ID = @HeaderId;";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@HeaderId", headerId);
                        cmd.Parameters.AddWithValue("@Status", processStatus);
                        cmd.Parameters.AddWithValue("@MaxRetry", _maxRetryCount > 0 ? _maxRetryCount : 3);
                        cmd.Parameters.AddWithValue("@ErrMsg", (object)errorMessage ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DocEntry", (object)docEntry ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("DB Update Stock Transfer Status Error: " + ex.Message);
            }
        }

        public bool IsStockTransferRetryLimitExceeded(long headerId)
        {
            if (string.IsNullOrEmpty(_connectionString)) return false;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"SELECT SOL_RETRYCOUNT FROM SOL_STOCK_TRANSFER_HEADER WHERE SOL_ID = @HeaderId";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@HeaderId", headerId);
                        object result = cmd.ExecuteScalar();
                        if (result == null || result == DBNull.Value) return false;
                        return Convert.ToInt32(result) >= _maxRetryCount;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        public void MarkStockTransferAsExceededRetryLimit(long headerId)
        {
            if (string.IsNullOrEmpty(_connectionString)) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"
                        UPDATE SOL_STOCK_TRANSFER_HEADER
                        SET SOL_PROCESS_STATUS = 3,
                            SOL_ERRORMESSAGE = 'Max retry limit exceeded',
                            SOL_UPDATED_AT = GETDATE()
                        WHERE SOL_ID = @HeaderId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@HeaderId", headerId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("DB MarkStockTransferExceeded Error: " + ex.Message);
            }
        }

        /// <summary>
        /// Mengambil Purchase Order pending dari tabel staging (SOL_PROCESS_STATUS = 0),
        /// termasuk detail barisnya, untuk disinkronkan ke SAP.
        /// </summary>
        public List<PendingPurchaseOrder> LoadPendingPurchaseOrders()
        {
            var result = new List<PendingPurchaseOrder>();
            if (string.IsNullOrEmpty(_connectionString)) return result;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string query = @"
                        SELECT h.SOL_ID AS HeaderId, h.SOL_CARDCODE AS CardCode, h.SOL_CARDNAME AS CardName,
                               h.SOL_DOCDATE AS DocDate, h.SOL_DOCDUEDATE AS DocDueDate, h.SOL_TAXDATE AS TaxDate,
                               h.SOL_REMARKS AS Remarks, h.SOL_WEB_TX_NUMBER AS WebTxNumber, h.SOL_WEB_TX_ID AS WebTxId,
                               h.SOL_UDF_DATA AS HeaderUdfData,
                               d.SOL_LINENUM AS LineNum, d.SOL_ITEMCODE AS ItemCode, d.SOL_ITEMNAME AS ItemName,
                               d.SOL_WAREHOUSE AS Warehouse, d.SOL_QUANTITY AS Quantity, d.SOL_PRICE AS Price,
                               d.SOL_VAT_GROUP AS VatGroup, d.SOL_WEB_LINE_ID AS WebLineId, d.SOL_UDF_DATA AS LineUdfData
                        FROM SOL_PURCHASE_ORDER_HEADER h
                        INNER JOIN SOL_PURCHASE_ORDER_DETAIL d ON d.SOL_HEADER_ID = h.SOL_ID
                        WHERE (h.SOL_PROCESS_STATUS = 0 
                               OR (h.SOL_PROCESS_STATUS = 2 AND h.SOL_RETRYCOUNT < 3 AND (
                                   (h.SOL_RETRYCOUNT = 1 AND DATEDIFF(second, ISNULL(h.SOL_UPDATED_AT, h.SOL_CREATED_AT), GETDATE()) >= 30)
                                   OR (h.SOL_RETRYCOUNT = 2 AND DATEDIFF(second, ISNULL(h.SOL_UPDATED_AT, h.SOL_CREATED_AT), GETDATE()) >= 120)
                               )))
                        ORDER BY h.SOL_ID, d.SOL_LINENUM";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        PendingPurchaseOrder current = null;
                        long currentHeaderId = -1;

                        while (reader.Read())
                        {
                            long headerId = Convert.ToInt64(reader["HeaderId"]);

                            if (current == null || headerId != currentHeaderId)
                            {
                                current = new PendingPurchaseOrder
                                {
                                    HeaderId = headerId,
                                    CardCode = reader["CardCode"]?.ToString() ?? "",
                                    CardName = reader["CardName"]?.ToString() ?? "",
                                    DocDate = Convert.IsDBNull(reader["DocDate"]) ? DateTime.Now : Convert.ToDateTime(reader["DocDate"]),
                                    DocDueDate = Convert.IsDBNull(reader["DocDueDate"]) ? DateTime.Now.AddDays(7) : Convert.ToDateTime(reader["DocDueDate"]),
                                    TaxDate = Convert.IsDBNull(reader["TaxDate"]) ? DateTime.Now : Convert.ToDateTime(reader["TaxDate"]),
                                    Remarks = reader["Remarks"]?.ToString() ?? "",
                                    WebTxNumber = Convert.IsDBNull(reader["WebTxNumber"]) ? null : reader["WebTxNumber"].ToString(),
                                    WebTxId = Convert.IsDBNull(reader["WebTxId"]) ? (long?)null : Convert.ToInt64(reader["WebTxId"]),
                                    UdfDataJson = Convert.IsDBNull(reader["HeaderUdfData"]) ? null : reader["HeaderUdfData"].ToString()
                                };
                                result.Add(current);
                                currentHeaderId = headerId;
                            }

                            current.Lines.Add(new PendingPurchaseOrderLine
                            {
                                LineNum = Convert.IsDBNull(reader["LineNum"]) ? 0 : Convert.ToInt32(reader["LineNum"]),
                                ItemCode = reader["ItemCode"]?.ToString() ?? "",
                                ItemName = reader["ItemName"]?.ToString() ?? "",
                                Warehouse = reader["Warehouse"]?.ToString() ?? "",
                                Quantity = Convert.IsDBNull(reader["Quantity"]) ? 0 : Convert.ToDecimal(reader["Quantity"]),
                                Price = Convert.IsDBNull(reader["Price"]) ? 0 : Convert.ToDecimal(reader["Price"]),
                                VatGroup = Convert.IsDBNull(reader["VatGroup"]) ? null : reader["VatGroup"].ToString(),
                                WebLineId = Convert.IsDBNull(reader["WebLineId"]) ? (long?)null : Convert.ToInt64(reader["WebLineId"]),
                                UdfDataJson = Convert.IsDBNull(reader["LineUdfData"]) ? null : reader["LineUdfData"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Gagal memuat Purchase Order pending dari staging.", ex);
            }

            return result;
        }

        /// <summary>
        /// Update status PO pending di tabel staging setelah proses sync ke SAP.
        /// SOL_PROCESS_STATUS: 0 = pending, 1 = sukses, 2 = gagal, 3 = dead letter.
        /// </summary>
        public void UpdatePurchaseOrderStatus(long headerId, int processStatus, string errorMessage = null, string docEntry = null)
        {
            if (string.IsNullOrEmpty(_connectionString)) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string query = @"
                        UPDATE SOL_PURCHASE_ORDER_HEADER
                        SET SOL_PROCESS_STATUS = CASE 
                                WHEN @Status = 1 THEN 1 
                                WHEN @Status = 2 AND (SOL_RETRYCOUNT + 1) >= @MaxRetry THEN 3 
                                ELSE @Status 
                            END,
                            SOL_ERRORMESSAGE = CASE 
                                WHEN @Status = 2 AND (SOL_RETRYCOUNT + 1) >= @MaxRetry THEN '[DEAD-LETTER] ' + ISNULL(@ErrMsg, 'Max retry limit reached')
                                ELSE @ErrMsg 
                            END,
                            SOL_DOCENTRY = CASE WHEN @DocEntry IS NOT NULL THEN @DocEntry ELSE SOL_DOCENTRY END,
                            SOL_RETRYCOUNT = CASE WHEN @Status = 1 THEN 0 ELSE SOL_RETRYCOUNT + 1 END,
                            SOL_PROCESSED_AT = GETDATE(),
                            SOL_UPDATED_AT = GETDATE()
                        WHERE SOL_ID = @HeaderId;

                        UPDATE SOL_PURCHASE_ORDER_DETAIL
                        SET SOL_PROCESS_STATUS = CASE 
                                WHEN @Status = 1 THEN 1 
                                WHEN @Status = 2 AND (SELECT SOL_RETRYCOUNT FROM SOL_PURCHASE_ORDER_HEADER WHERE SOL_ID = @HeaderId) >= @MaxRetry THEN 3 
                                ELSE @Status 
                            END,
                            SOL_UPDATED_AT = GETDATE()
                        WHERE SOL_HEADER_ID = @HeaderId;";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@HeaderId", headerId);
                        cmd.Parameters.AddWithValue("@Status", processStatus);
                        cmd.Parameters.AddWithValue("@MaxRetry", _maxRetryCount > 0 ? _maxRetryCount : 3);
                        cmd.Parameters.AddWithValue("@ErrMsg", (object)errorMessage ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DocEntry", (object)docEntry ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("DB Update Purchase Order Status Error: " + ex.Message);
            }
        }

        /// <summary>
        /// Returns true if the PO header has exceeded the max retry limit.
        /// </summary>
        public bool IsPurchaseOrderRetryLimitExceeded(long headerId)
        {
            if (string.IsNullOrEmpty(_connectionString)) return false;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"SELECT SOL_RETRYCOUNT FROM SOL_PURCHASE_ORDER_HEADER WHERE SOL_ID = @HeaderId";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@HeaderId", headerId);
                        object result = cmd.ExecuteScalar();
                        if (result == null || result == DBNull.Value) return false;
                        int retryCount = Convert.ToInt32(result);
                        return retryCount >= _maxRetryCount;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Mark PO as dead-letter (status = 3) when retry limit exceeded.
        /// </summary>
        public void MarkPurchaseOrderAsExceededRetryLimit(long headerId)
        {
            if (string.IsNullOrEmpty(_connectionString)) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"
                        UPDATE SOL_PURCHASE_ORDER_HEADER
                        SET SOL_PROCESS_STATUS = 3,
                            SOL_ERRORMESSAGE = 'Max retry limit exceeded',
                            SOL_UPDATED_AT = GETDATE()
                        WHERE SOL_ID = @HeaderId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@HeaderId", headerId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("DB MarkExceeded Error: " + ex.Message);
            }
        }

        public List<SyncLogModel> GetLatestUnresolvedErrors()
        {
            var latestErrors = new List<SyncLogModel>();
            if (string.IsNullOrEmpty(_connectionString)) return latestErrors;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT ErrorID, DocType, DocEntry, CardCode, ItemCode, Quantity, Price, WarehouseCode, ErrorMessage, CreatedAt
                        FROM TBL_SYNC_ERROR e1
                        WHERE IsResolved = 0
                        AND ErrorID = (
                            SELECT MAX(ErrorID)
                            FROM TBL_SYNC_ERROR e2
                            WHERE e2.CardCode = e1.CardCode
                              AND e2.ItemCode = e1.ItemCode
                              AND e2.IsResolved = 0
                        )";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            latestErrors.Add(new SyncLogModel
                            {
                                ErrorId = Convert.ToInt32(reader["ErrorID"]),
                                DocType = reader["DocType"]?.ToString() ?? "",
                                DocEntry = reader["DocEntry"]?.ToString() ?? "",
                                CardCode = reader["CardCode"]?.ToString() ?? "",
                                ItemCode = reader["ItemCode"]?.ToString() ?? "",
                                Quantity = Convert.IsDBNull(reader["Quantity"]) ? 0 : Convert.ToDouble(reader["Quantity"]),
                                Price = Convert.IsDBNull(reader["Price"]) ? 0.0 : Convert.ToDouble(reader["Price"]),
                                WarehouseCode = reader["WarehouseCode"]?.ToString() ?? "",
                                ErrorMessage = reader["ErrorMessage"]?.ToString() ?? "",
                                CreatedAt = Convert.IsDBNull(reader["CreatedAt"]) ? DateTime.MinValue : Convert.ToDateTime(reader["CreatedAt"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Gagal memuat data error dari database.", ex);
            }
            return latestErrors;
        }

        public List<DocumentHeaderLogModel> LoadDocumentHeaderLogs()
        {
            var list = new List<DocumentHeaderLogModel>();
            if (string.IsNullOrEmpty(_connectionString)) return list;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Query Purchase Orders
                    try
                    {
                        string queryPO = @"
                            SELECT SOL_ID AS HeaderId, 'Purchase Order' AS DocType,
                                   ISNULL(SOL_WEB_TX_NUMBER, '') AS WebTxNumber,
                                   ISNULL(SOL_CARDCODE, '') AS CardCode,
                                   ISNULL(SOL_CARDNAME, '') AS CardName,
                                   SOL_DOCDATE AS DocDate,
                                   SOL_DOCDUEDATE AS DocDueDate,
                                   SOL_PROCESS_STATUS AS ProcessStatus,
                                   ISNULL(SOL_DOCENTRY, '') AS DocEntry,
                                   SOL_PROCESSED_AT AS ProcessedAt,
                                   SOL_CREATED_AT AS CreatedAt,
                                   ISNULL(SOL_REMARKS, '') AS Remarks,
                                   ISNULL(SOL_ERRORMESSAGE, '') AS ErrorMessage,
                                   SOL_UDF_DATA AS UdfDataJson
                            FROM SOL_PURCHASE_ORDER_HEADER";

                        using (SqlCommand cmd = new SqlCommand(queryPO, conn))
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int statusVal = Convert.IsDBNull(reader["ProcessStatus"]) ? 0 : Convert.ToInt32(reader["ProcessStatus"]);
                                string statusStr = "Pending";
                                if (statusVal == 1) statusStr = "Success";
                                else if (statusVal == 2) statusStr = "Failed";
                                else if (statusVal == 3) statusStr = "Cancelled";

                                list.Add(new DocumentHeaderLogModel
                                {
                                    HeaderId = Convert.ToInt64(reader["HeaderId"]),
                                    DocType = reader["DocType"].ToString(),
                                    WebTxNumber = reader["WebTxNumber"].ToString(),
                                    CardCode = reader["CardCode"].ToString(),
                                    CardName = reader["CardName"].ToString(),
                                    DocDate = Convert.IsDBNull(reader["DocDate"]) ? DateTime.MinValue : Convert.ToDateTime(reader["DocDate"]),
                                    DocDueDate = Convert.IsDBNull(reader["DocDueDate"]) ? DateTime.MinValue : Convert.ToDateTime(reader["DocDueDate"]),
                                    Status = statusStr,
                                    DocEntry = reader["DocEntry"].ToString(),
                                    ProcessedAt = Convert.IsDBNull(reader["ProcessedAt"]) ? (DateTime?)null : Convert.ToDateTime(reader["ProcessedAt"]),
                                    CreatedAt = Convert.IsDBNull(reader["CreatedAt"]) ? DateTime.MinValue : Convert.ToDateTime(reader["CreatedAt"]),
                                    Remarks = reader["Remarks"].ToString(),
                                    ErrorMessage = reader["ErrorMessage"].ToString(),
                                    UdfDataJson = Convert.IsDBNull(reader["UdfDataJson"]) ? null : reader["UdfDataJson"].ToString()
                                });
                            }
                        }
                    }
                    catch (Exception exPO)
                    {
                        Console.WriteLine("LoadDocumentHeaderLogs PO Error: " + exPO.Message);
                    }

                    // Query Goods Receipt PO
                    try
                    {
                        string queryGRPO = @"
                            SELECT SOL_ID AS HeaderId, 'Goods Receipt PO' AS DocType,
                                   ISNULL(SOL_WEB_TX_NUMBER, '') AS WebTxNumber,
                                   ISNULL(SOL_CARDCODE, '') AS CardCode,
                                   ISNULL(SOL_CARDNAME, '') AS CardName,
                                   SOL_DOCDATE AS DocDate,
                                   SOL_DOCDUEDATE AS DocDueDate,
                                   SOL_PROCESS_STATUS AS ProcessStatus,
                                   ISNULL(SOL_DOCENTRY, '') AS DocEntry,
                                   SOL_PROCESSED_AT AS ProcessedAt,
                                   SOL_CREATED_AT AS CreatedAt,
                                   ISNULL(SOL_REMARKS, '') AS Remarks,
                                   ISNULL(SOL_ERRORMESSAGE, '') AS ErrorMessage,
                                   SOL_UDF_DATA AS UdfDataJson
                            FROM SOL_GRPO_HEADER";

                        using (SqlCommand cmd = new SqlCommand(queryGRPO, conn))
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int statusVal = Convert.IsDBNull(reader["ProcessStatus"]) ? 0 : Convert.ToInt32(reader["ProcessStatus"]);
                                string statusStr = "Pending";
                                if (statusVal == 1) statusStr = "Success";
                                else if (statusVal == 2) statusStr = "Failed";
                                else if (statusVal == 3) statusStr = "Cancelled";

                                list.Add(new DocumentHeaderLogModel
                                {
                                    HeaderId = Convert.ToInt64(reader["HeaderId"]),
                                    DocType = reader["DocType"].ToString(),
                                    WebTxNumber = reader["WebTxNumber"].ToString(),
                                    CardCode = reader["CardCode"].ToString(),
                                    CardName = reader["CardName"].ToString(),
                                    DocDate = Convert.IsDBNull(reader["DocDate"]) ? DateTime.MinValue : Convert.ToDateTime(reader["DocDate"]),
                                    DocDueDate = Convert.IsDBNull(reader["DocDueDate"]) ? DateTime.MinValue : Convert.ToDateTime(reader["DocDueDate"]),
                                    Status = statusStr,
                                    DocEntry = reader["DocEntry"].ToString(),
                                    ProcessedAt = Convert.IsDBNull(reader["ProcessedAt"]) ? (DateTime?)null : Convert.ToDateTime(reader["ProcessedAt"]),
                                    CreatedAt = Convert.IsDBNull(reader["CreatedAt"]) ? DateTime.MinValue : Convert.ToDateTime(reader["CreatedAt"]),
                                    Remarks = reader["Remarks"].ToString(),
                                    ErrorMessage = reader["ErrorMessage"].ToString(),
                                    UdfDataJson = Convert.IsDBNull(reader["UdfDataJson"]) ? null : reader["UdfDataJson"].ToString()
                                });
                            }
                        }
                    }
                    catch (Exception exGRPO)
                    {
                        Console.WriteLine("LoadDocumentHeaderLogs GRPO Error: " + exGRPO.Message);
                    }

                    // Query Stock Transfer
                    try
                    {
                        string queryTransfer = @"
                            SELECT SOL_ID AS HeaderId, 'Stock Transfer' AS DocType,
                                   ISNULL(SOL_WEB_TX_NUMBER, '') AS WebTxNumber,
                                   ISNULL(SOL_CARDCODE, '') AS CardCode,
                                   ISNULL(SOL_CARDNAME, '') AS CardName,
                                   SOL_DOCDATE AS DocDate,
                                   SOL_DOCDUEDATE AS DocDueDate,
                                   SOL_PROCESS_STATUS AS ProcessStatus,
                                   ISNULL(SOL_DOCENTRY, '') AS DocEntry,
                                   SOL_PROCESSED_AT AS ProcessedAt,
                                   SOL_CREATED_AT AS CreatedAt,
                                   ISNULL(SOL_REMARKS, '') AS Remarks,
                                   ISNULL(SOL_ERRORMESSAGE, '') AS ErrorMessage,
                                   SOL_UDF_DATA AS UdfDataJson
                            FROM SOL_STOCK_TRANSFER_HEADER";

                        using (SqlCommand cmd = new SqlCommand(queryTransfer, conn))
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int statusVal = Convert.IsDBNull(reader["ProcessStatus"]) ? 0 : Convert.ToInt32(reader["ProcessStatus"]);
                                string statusStr = "Pending";
                                if (statusVal == 1) statusStr = "Success";
                                else if (statusVal == 2) statusStr = "Failed";
                                else if (statusVal == 3) statusStr = "Cancelled";

                                list.Add(new DocumentHeaderLogModel
                                {
                                    HeaderId = Convert.ToInt64(reader["HeaderId"]),
                                    DocType = reader["DocType"].ToString(),
                                    WebTxNumber = reader["WebTxNumber"].ToString(),
                                    CardCode = reader["CardCode"].ToString(),
                                    CardName = reader["CardName"].ToString(),
                                    DocDate = Convert.IsDBNull(reader["DocDate"]) ? DateTime.MinValue : Convert.ToDateTime(reader["DocDate"]),
                                    DocDueDate = Convert.IsDBNull(reader["DocDueDate"]) ? DateTime.MinValue : Convert.ToDateTime(reader["DocDueDate"]),
                                    Status = statusStr,
                                    DocEntry = reader["DocEntry"].ToString(),
                                    ProcessedAt = Convert.IsDBNull(reader["ProcessedAt"]) ? (DateTime?)null : Convert.ToDateTime(reader["ProcessedAt"]),
                                    CreatedAt = Convert.IsDBNull(reader["CreatedAt"]) ? DateTime.MinValue : Convert.ToDateTime(reader["CreatedAt"]),
                                    Remarks = reader["Remarks"].ToString(),
                                    ErrorMessage = reader["ErrorMessage"].ToString(),
                                    UdfDataJson = Convert.IsDBNull(reader["UdfDataJson"]) ? null : reader["UdfDataJson"].ToString()
                                });
                            }
                        }
                    }
                    catch (Exception exTransfer)
                    {
                        Console.WriteLine("LoadDocumentHeaderLogs Stock Transfer Error: " + exTransfer.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("LoadDocumentHeaderLogs General Error: " + ex.Message);
            }

            // Urutkan dari yang terbaru
            list.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));
            return list;
        }

        public List<DocumentDetailLineModel> LoadPurchaseOrderLineDetails(long headerId)
        {
            var lines = new List<DocumentDetailLineModel>();
            if (string.IsNullOrEmpty(_connectionString)) return lines;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT SOL_LINENUM AS LineNum,
                               ISNULL(SOL_ITEMCODE, '') AS ItemCode,
                               ISNULL(SOL_ITEMNAME, '') AS ItemName,
                               ISNULL(SOL_WAREHOUSE, '') AS Warehouse,
                               ISNULL(SOL_QUANTITY, 0) AS Quantity,
                               ISNULL(SOL_PRICE, 0) AS Price,
                               ISNULL(SOL_VAT_GROUP, '') AS VatGroup,
                               SOL_PROCESS_STATUS AS ProcessStatus,
                               ISNULL(SOL_ERRORMESSAGE, '') AS ErrorMessage,
                               SOL_UDF_DATA AS UdfDataJson
                        FROM SOL_PURCHASE_ORDER_DETAIL
                        WHERE SOL_HEADER_ID = @HeaderId
                        ORDER BY SOL_LINENUM";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@HeaderId", headerId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int statusVal = Convert.IsDBNull(reader["ProcessStatus"]) ? 0 : Convert.ToInt32(reader["ProcessStatus"]);
                                string statusStr = "Pending";
                                if (statusVal == 1) statusStr = "Success";
                                else if (statusVal == 2) statusStr = "Failed";
                                else if (statusVal == 3) statusStr = "Cancelled";

                                lines.Add(new DocumentDetailLineModel
                                {
                                    LineNum = Convert.ToInt32(reader["LineNum"]),
                                    ItemCode = reader["ItemCode"].ToString(),
                                    ItemName = reader["ItemName"].ToString(),
                                    Warehouse = reader["Warehouse"].ToString(),
                                    Quantity = Convert.ToDecimal(reader["Quantity"]),
                                    Price = Convert.ToDecimal(reader["Price"]),
                                    VatGroup = reader["VatGroup"].ToString(),
                                    Status = statusStr,
                                    ErrorMessage = reader["ErrorMessage"].ToString(),
                                    UdfDataJson = Convert.IsDBNull(reader["UdfDataJson"]) ? null : reader["UdfDataJson"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("LoadPurchaseOrderLineDetails Error: " + ex.Message);
            }

            return lines;
        }

        public List<DocumentDetailLineModel> LoadGoodsReceiptPOLineDetails(long headerId)
        {
            var lines = new List<DocumentDetailLineModel>();
            if (string.IsNullOrEmpty(_connectionString)) return lines;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT SOL_LINENUM AS LineNum,
                               ISNULL(SOL_ITEMCODE, '') AS ItemCode,
                               ISNULL(SOL_ITEMNAME, '') AS ItemName,
                               ISNULL(SOL_WAREHOUSE, '') AS Warehouse,
                               ISNULL(SOL_QUANTITY, 0) AS Quantity,
                               ISNULL(SOL_PRICE, 0) AS Price,
                               ISNULL(SOL_VAT_GROUP, '') AS VatGroup,
                               SOL_PROCESS_STATUS AS ProcessStatus,
                               ISNULL(SOL_ERRORMESSAGE, '') AS ErrorMessage,
                               SOL_UDF_DATA AS UdfDataJson
                        FROM SOL_GRPO_DETAIL
                        WHERE SOL_HEADER_ID = @HeaderId
                        ORDER BY SOL_LINENUM";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@HeaderId", headerId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int statusVal = Convert.IsDBNull(reader["ProcessStatus"]) ? 0 : Convert.ToInt32(reader["ProcessStatus"]);
                                string statusStr = "Pending";
                                if (statusVal == 1) statusStr = "Success";
                                else if (statusVal == 2) statusStr = "Failed";
                                else if (statusVal == 3) statusStr = "Cancelled";

                                lines.Add(new DocumentDetailLineModel
                                {
                                    LineNum = Convert.ToInt32(reader["LineNum"]),
                                    ItemCode = reader["ItemCode"].ToString(),
                                    ItemName = reader["ItemName"].ToString(),
                                    Warehouse = reader["Warehouse"].ToString(),
                                    Quantity = Convert.ToDecimal(reader["Quantity"]),
                                    Price = Convert.ToDecimal(reader["Price"]),
                                    VatGroup = reader["VatGroup"].ToString(),
                                    Status = statusStr,
                                    ErrorMessage = reader["ErrorMessage"].ToString(),
                                    UdfDataJson = Convert.IsDBNull(reader["UdfDataJson"]) ? null : reader["UdfDataJson"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("LoadGoodsReceiptPOLineDetails Error: " + ex.Message);
            }

            return lines;
        }

        public List<DocumentDetailLineModel> LoadStockTransferLineDetails(long headerId)
        {
            var lines = new List<DocumentDetailLineModel>();
            if (string.IsNullOrEmpty(_connectionString)) return lines;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT SOL_LINENUM AS LineNum,
                               ISNULL(SOL_ITEMCODE, '') AS ItemCode,
                               ISNULL(SOL_ITEMNAME, '') AS ItemName,
                               ISNULL(SOL_WAREHOUSE, '') AS Warehouse,
                               ISNULL(SOL_QUANTITY, 0) AS Quantity,
                               ISNULL(SOL_PRICE, 0) AS Price,
                               '' AS VatGroup,
                               SOL_PROCESS_STATUS AS ProcessStatus,
                               ISNULL(SOL_ERRORMESSAGE, '') AS ErrorMessage,
                               SOL_UDF_DATA AS UdfDataJson
                        FROM SOL_STOCK_TRANSFER_DETAIL
                        WHERE SOL_HEADER_ID = @HeaderId
                        ORDER BY SOL_LINENUM";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@HeaderId", headerId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int statusVal = Convert.IsDBNull(reader["ProcessStatus"]) ? 0 : Convert.ToInt32(reader["ProcessStatus"]);
                                string statusStr = "Pending";
                                if (statusVal == 1) statusStr = "Success";
                                else if (statusVal == 2) statusStr = "Failed";
                                else if (statusVal == 3) statusStr = "Cancelled";

                                lines.Add(new DocumentDetailLineModel
                                {
                                    LineNum = Convert.ToInt32(reader["LineNum"]),
                                    ItemCode = reader["ItemCode"].ToString(),
                                    ItemName = reader["ItemName"].ToString(),
                                    Warehouse = reader["Warehouse"].ToString(),
                                    Quantity = Convert.ToDecimal(reader["Quantity"]),
                                    Price = Convert.ToDecimal(reader["Price"]),
                                    VatGroup = "",
                                    Status = statusStr,
                                    ErrorMessage = reader["ErrorMessage"].ToString(),
                                    UdfDataJson = Convert.IsDBNull(reader["UdfDataJson"]) ? null : reader["UdfDataJson"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("LoadStockTransferLineDetails Error: " + ex.Message);
            }

            return lines;
        }

        /// <summary>
        /// Mengambil antrean dokumen yang belum tersinkronisasi ke SAP (Pending = 0 atau Failed = 2).
        /// Filter modul aktif berdasarkan checkbox pada tab pertama (PO / GRPO / Transfer / SL).
        /// </summary>
        public List<PendingQueueDocModel> LoadUnsyncedDocuments(bool includePO, bool includeGRPO, bool includeTransfer, bool includeSL)
        {
            var list = new List<PendingQueueDocModel>();
            if (string.IsNullOrEmpty(_connectionString)) return list;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // 1. Purchase Orders (SOL_PURCHASE_ORDER_HEADER)
                    if (includePO || includeSL)
                    {
                        try
                        {
                            string queryPO = @"
                                SELECT SOL_ID AS HeaderId, 'Purchase Order' AS DocType,
                                       ISNULL(SOL_WEB_TX_NUMBER, '') AS WebTxNumber,
                                       ISNULL(SOL_CARDCODE, '') AS CardCode,
                                       ISNULL(SOL_CARDNAME, '') AS CardName,
                                       SOL_DOCDATE AS DocDate,
                                       SOL_DOCDUEDATE AS DocDueDate,
                                       SOL_PROCESS_STATUS AS ProcessStatus,
                                       SOL_CREATED_AT AS CreatedAt,
                                       ISNULL(SOL_REMARKS, '') AS Remarks,
                                       ISNULL(SOL_ERRORMESSAGE, '') AS ErrorMessage,
                                       SOL_UDF_DATA AS UdfDataJson
                                FROM SOL_PURCHASE_ORDER_HEADER
                                WHERE SOL_PROCESS_STATUS IN (0, 2)";

                            using (SqlCommand cmd = new SqlCommand(queryPO, conn))
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    int statusVal = Convert.IsDBNull(reader["ProcessStatus"]) ? 0 : Convert.ToInt32(reader["ProcessStatus"]);
                                    string statusStr = statusVal == 0 ? "Pending" : "Failed";

                                    list.Add(new PendingQueueDocModel
                                    {
                                        IsSelected = true,
                                        HeaderId = Convert.ToInt64(reader["HeaderId"]),
                                        DocType = reader["DocType"].ToString(),
                                        WebTxNumber = reader["WebTxNumber"].ToString(),
                                        CardCode = reader["CardCode"].ToString(),
                                        CardName = reader["CardName"].ToString(),
                                        DocDate = Convert.IsDBNull(reader["DocDate"]) ? DateTime.MinValue : Convert.ToDateTime(reader["DocDate"]),
                                        DocDueDate = Convert.IsDBNull(reader["DocDueDate"]) ? DateTime.MinValue : Convert.ToDateTime(reader["DocDueDate"]),
                                        ProcessStatus = statusVal,
                                        Status = statusStr,
                                        CreatedAt = Convert.IsDBNull(reader["CreatedAt"]) ? DateTime.MinValue : Convert.ToDateTime(reader["CreatedAt"]),
                                        Remarks = reader["Remarks"].ToString(),
                                        ErrorMessage = reader["ErrorMessage"].ToString(),
                                        UdfDataJson = Convert.IsDBNull(reader["UdfDataJson"]) ? null : reader["UdfDataJson"].ToString()
                                    });
                                }
                            }
                        }
                        catch (Exception exPO)
                        {
                            Console.WriteLine("LoadUnsyncedDocuments PO Error: " + exPO.Message);
                        }
                    }

                    // 2. Goods Receipt PO (SOL_GRPO_HEADER)
                    if (includeGRPO)
                    {
                        try
                        {
                            string queryGRPO = @"
                                SELECT SOL_ID AS HeaderId, 'Goods Receipt PO' AS DocType,
                                       ISNULL(SOL_WEB_TX_NUMBER, '') AS WebTxNumber,
                                       ISNULL(SOL_CARDCODE, '') AS CardCode,
                                       ISNULL(SOL_CARDNAME, '') AS CardName,
                                       SOL_DOCDATE AS DocDate,
                                       SOL_DOCDUEDATE AS DocDueDate,
                                       SOL_PROCESS_STATUS AS ProcessStatus,
                                       SOL_CREATED_AT AS CreatedAt,
                                       ISNULL(SOL_REMARKS, '') AS Remarks,
                                       ISNULL(SOL_ERRORMESSAGE, '') AS ErrorMessage,
                                       SOL_UDF_DATA AS UdfDataJson
                                FROM SOL_GRPO_HEADER
                                WHERE SOL_PROCESS_STATUS IN (0, 2)";

                            using (SqlCommand cmd = new SqlCommand(queryGRPO, conn))
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    int statusVal = Convert.IsDBNull(reader["ProcessStatus"]) ? 0 : Convert.ToInt32(reader["ProcessStatus"]);
                                    string statusStr = statusVal == 0 ? "Pending" : "Failed";

                                    list.Add(new PendingQueueDocModel
                                    {
                                        IsSelected = true,
                                        HeaderId = Convert.ToInt64(reader["HeaderId"]),
                                        DocType = reader["DocType"].ToString(),
                                        WebTxNumber = reader["WebTxNumber"].ToString(),
                                        CardCode = reader["CardCode"].ToString(),
                                        CardName = reader["CardName"].ToString(),
                                        DocDate = Convert.IsDBNull(reader["DocDate"]) ? DateTime.MinValue : Convert.ToDateTime(reader["DocDate"]),
                                        DocDueDate = Convert.IsDBNull(reader["DocDueDate"]) ? DateTime.MinValue : Convert.ToDateTime(reader["DocDueDate"]),
                                        ProcessStatus = statusVal,
                                        Status = statusStr,
                                        CreatedAt = Convert.IsDBNull(reader["CreatedAt"]) ? DateTime.MinValue : Convert.ToDateTime(reader["CreatedAt"]),
                                        Remarks = reader["Remarks"].ToString(),
                                        ErrorMessage = reader["ErrorMessage"].ToString(),
                                        UdfDataJson = Convert.IsDBNull(reader["UdfDataJson"]) ? null : reader["UdfDataJson"].ToString()
                                    });
                                }
                            }
                        }
                        catch (Exception exGRPO)
                        {
                            Console.WriteLine("LoadUnsyncedDocuments GRPO Error: " + exGRPO.Message);
                        }
                    }

                    // 3. Stock Transfer (SOL_STOCK_TRANSFER_HEADER)
                    if (includeTransfer)
                    {
                        try
                        {
                            string queryTransfer = @"
                                SELECT SOL_ID AS HeaderId, 'Stock Transfer' AS DocType,
                                       ISNULL(SOL_WEB_TX_NUMBER, '') AS WebTxNumber,
                                       ISNULL(SOL_CARDCODE, '') AS CardCode,
                                       ISNULL(SOL_CARDNAME, '') AS CardName,
                                       SOL_DOCDATE AS DocDate,
                                       SOL_DOCDUEDATE AS DocDueDate,
                                       SOL_PROCESS_STATUS AS ProcessStatus,
                                       SOL_CREATED_AT AS CreatedAt,
                                       ISNULL(SOL_REMARKS, '') AS Remarks,
                                       ISNULL(SOL_ERRORMESSAGE, '') AS ErrorMessage,
                                       SOL_UDF_DATA AS UdfDataJson
                                FROM SOL_STOCK_TRANSFER_HEADER
                                WHERE SOL_PROCESS_STATUS IN (0, 2)";

                            using (SqlCommand cmd = new SqlCommand(queryTransfer, conn))
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    int statusVal = Convert.IsDBNull(reader["ProcessStatus"]) ? 0 : Convert.ToInt32(reader["ProcessStatus"]);
                                    string statusStr = statusVal == 0 ? "Pending" : "Failed";

                                    list.Add(new PendingQueueDocModel
                                    {
                                        IsSelected = true,
                                        HeaderId = Convert.ToInt64(reader["HeaderId"]),
                                        DocType = reader["DocType"].ToString(),
                                        WebTxNumber = reader["WebTxNumber"].ToString(),
                                        CardCode = reader["CardCode"].ToString(),
                                        CardName = reader["CardName"].ToString(),
                                        DocDate = Convert.IsDBNull(reader["DocDate"]) ? DateTime.MinValue : Convert.ToDateTime(reader["DocDate"]),
                                        DocDueDate = Convert.IsDBNull(reader["DocDueDate"]) ? DateTime.MinValue : Convert.ToDateTime(reader["DocDueDate"]),
                                        ProcessStatus = statusVal,
                                        Status = statusStr,
                                        CreatedAt = Convert.IsDBNull(reader["CreatedAt"]) ? DateTime.MinValue : Convert.ToDateTime(reader["CreatedAt"]),
                                        Remarks = reader["Remarks"].ToString(),
                                        ErrorMessage = reader["ErrorMessage"].ToString(),
                                        UdfDataJson = Convert.IsDBNull(reader["UdfDataJson"]) ? null : reader["UdfDataJson"].ToString()
                                    });
                                }
                            }
                        }
                        catch (Exception exTransfer)
                        {
                            Console.WriteLine("LoadUnsyncedDocuments Stock Transfer Error: " + exTransfer.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("LoadUnsyncedDocuments General Error: " + ex.Message);
            }

            list.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));
            return list;
        }

        public List<PendingPurchaseOrder> LoadPendingPurchaseOrdersByIds(IEnumerable<long> headerIds)
        {
            var result = new List<PendingPurchaseOrder>();
            if (string.IsNullOrEmpty(_connectionString) || headerIds == null) return result;

            var idList = new List<long>(headerIds);
            if (idList.Count == 0) return result;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string idsJoined = string.Join(",", idList);

                    string query = $@"
                        SELECT h.SOL_ID AS HeaderId, h.SOL_CARDCODE AS CardCode, h.SOL_CARDNAME AS CardName,
                               h.SOL_DOCDATE AS DocDate, h.SOL_DOCDUEDATE AS DocDueDate, h.SOL_TAXDATE AS TaxDate,
                               h.SOL_REMARKS AS Remarks, h.SOL_WEB_TX_NUMBER AS WebTxNumber, h.SOL_WEB_TX_ID AS WebTxId,
                               h.SOL_UDF_DATA AS HeaderUdfData,
                               d.SOL_LINENUM AS LineNum, d.SOL_ITEMCODE AS ItemCode, d.SOL_ITEMNAME AS ItemName,
                               d.SOL_WAREHOUSE AS Warehouse, d.SOL_QUANTITY AS Quantity, d.SOL_PRICE AS Price,
                               d.SOL_VAT_GROUP AS VatGroup, d.SOL_WEB_LINE_ID AS WebLineId, d.SOL_UDF_DATA AS LineUdfData
                        FROM SOL_PURCHASE_ORDER_HEADER h
                        INNER JOIN SOL_PURCHASE_ORDER_DETAIL d ON d.SOL_HEADER_ID = h.SOL_ID
                        WHERE h.SOL_ID IN ({idsJoined}) AND h.SOL_PROCESS_STATUS IN (0, 2)
                        ORDER BY h.SOL_ID, d.SOL_LINENUM";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        PendingPurchaseOrder current = null;
                        long currentHeaderId = -1;

                        while (reader.Read())
                        {
                            long headerId = Convert.ToInt64(reader["HeaderId"]);

                            if (current == null || headerId != currentHeaderId)
                            {
                                current = new PendingPurchaseOrder
                                {
                                    HeaderId = headerId,
                                    CardCode = reader["CardCode"]?.ToString() ?? "",
                                    CardName = reader["CardName"]?.ToString() ?? "",
                                    DocDate = Convert.IsDBNull(reader["DocDate"]) ? DateTime.Now : Convert.ToDateTime(reader["DocDate"]),
                                    DocDueDate = Convert.IsDBNull(reader["DocDueDate"]) ? DateTime.Now.AddDays(7) : Convert.ToDateTime(reader["DocDueDate"]),
                                    TaxDate = Convert.IsDBNull(reader["TaxDate"]) ? DateTime.Now : Convert.ToDateTime(reader["TaxDate"]),
                                    Remarks = reader["Remarks"]?.ToString() ?? "",
                                    WebTxNumber = Convert.IsDBNull(reader["WebTxNumber"]) ? null : reader["WebTxNumber"].ToString(),
                                    WebTxId = Convert.IsDBNull(reader["WebTxId"]) ? (long?)null : Convert.ToInt64(reader["WebTxId"]),
                                    UdfDataJson = Convert.IsDBNull(reader["HeaderUdfData"]) ? null : reader["HeaderUdfData"].ToString()
                                };
                                result.Add(current);
                                currentHeaderId = headerId;
                            }

                            current.Lines.Add(new PendingPurchaseOrderLine
                            {
                                LineNum = Convert.IsDBNull(reader["LineNum"]) ? 0 : Convert.ToInt32(reader["LineNum"]),
                                ItemCode = reader["ItemCode"]?.ToString() ?? "",
                                ItemName = reader["ItemName"]?.ToString() ?? "",
                                Warehouse = reader["Warehouse"]?.ToString() ?? "",
                                Quantity = Convert.IsDBNull(reader["Quantity"]) ? 0 : Convert.ToDecimal(reader["Quantity"]),
                                Price = Convert.IsDBNull(reader["Price"]) ? 0 : Convert.ToDecimal(reader["Price"]),
                                VatGroup = Convert.IsDBNull(reader["VatGroup"]) ? null : reader["VatGroup"].ToString(),
                                WebLineId = Convert.IsDBNull(reader["WebLineId"]) ? (long?)null : Convert.ToInt64(reader["WebLineId"]),
                                UdfDataJson = Convert.IsDBNull(reader["LineUdfData"]) ? null : reader["LineUdfData"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Gagal memuat Purchase Order terpilih dari staging.", ex);
            }

            return result;
        }

        public List<PendingPurchaseOrder> LoadPendingGoodsReceiptPOsByIds(IEnumerable<long> headerIds)
        {
            var result = new List<PendingPurchaseOrder>();
            if (string.IsNullOrEmpty(_connectionString) || headerIds == null) return result;

            var idList = new List<long>(headerIds);
            if (idList.Count == 0) return result;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string idsJoined = string.Join(",", idList);

                    string query = $@"
                        SELECT h.SOL_ID AS HeaderId, h.SOL_CARDCODE AS CardCode, h.SOL_CARDNAME AS CardName,
                               h.SOL_DOCDATE AS DocDate, h.SOL_DOCDUEDATE AS DocDueDate, h.SOL_TAXDATE AS TaxDate,
                               h.SOL_REMARKS AS Remarks, h.SOL_WEB_TX_NUMBER AS WebTxNumber, h.SOL_WEB_TX_ID AS WebTxId,
                               h.SOL_UDF_DATA AS HeaderUdfData,
                               d.SOL_LINENUM AS LineNum, d.SOL_ITEMCODE AS ItemCode, d.SOL_ITEMNAME AS ItemName,
                               d.SOL_WAREHOUSE AS Warehouse, d.SOL_QUANTITY AS Quantity, d.SOL_PRICE AS Price,
                               d.SOL_VAT_GROUP AS VatGroup, d.SOL_WEB_LINE_ID AS WebLineId, d.SOL_UDF_DATA AS LineUdfData
                        FROM SOL_GRPO_HEADER h
                        INNER JOIN SOL_GRPO_DETAIL d ON d.SOL_HEADER_ID = h.SOL_ID
                        WHERE h.SOL_ID IN ({idsJoined}) AND h.SOL_PROCESS_STATUS IN (0, 2)
                        ORDER BY h.SOL_ID, d.SOL_LINENUM";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        PendingPurchaseOrder current = null;
                        long currentHeaderId = -1;

                        while (reader.Read())
                        {
                            long headerId = Convert.ToInt64(reader["HeaderId"]);

                            if (current == null || headerId != currentHeaderId)
                            {
                                current = new PendingPurchaseOrder
                                {
                                    HeaderId = headerId,
                                    CardCode = reader["CardCode"]?.ToString() ?? "",
                                    CardName = reader["CardName"]?.ToString() ?? "",
                                    DocDate = Convert.IsDBNull(reader["DocDate"]) ? DateTime.Now : Convert.ToDateTime(reader["DocDate"]),
                                    DocDueDate = Convert.IsDBNull(reader["DocDueDate"]) ? DateTime.Now.AddDays(7) : Convert.ToDateTime(reader["DocDueDate"]),
                                    TaxDate = Convert.IsDBNull(reader["TaxDate"]) ? DateTime.Now : Convert.ToDateTime(reader["TaxDate"]),
                                    Remarks = reader["Remarks"]?.ToString() ?? "",
                                    WebTxNumber = Convert.IsDBNull(reader["WebTxNumber"]) ? null : reader["WebTxNumber"].ToString(),
                                    WebTxId = Convert.IsDBNull(reader["WebTxId"]) ? (long?)null : Convert.ToInt64(reader["WebTxId"]),
                                    UdfDataJson = Convert.IsDBNull(reader["HeaderUdfData"]) ? null : reader["HeaderUdfData"].ToString()
                                };
                                result.Add(current);
                                currentHeaderId = headerId;
                            }

                            current.Lines.Add(new PendingPurchaseOrderLine
                            {
                                LineNum = Convert.IsDBNull(reader["LineNum"]) ? 0 : Convert.ToInt32(reader["LineNum"]),
                                ItemCode = reader["ItemCode"]?.ToString() ?? "",
                                ItemName = reader["ItemName"]?.ToString() ?? "",
                                Warehouse = reader["Warehouse"]?.ToString() ?? "",
                                Quantity = Convert.IsDBNull(reader["Quantity"]) ? 0 : Convert.ToDecimal(reader["Quantity"]),
                                Price = Convert.IsDBNull(reader["Price"]) ? 0 : Convert.ToDecimal(reader["Price"]),
                                VatGroup = Convert.IsDBNull(reader["VatGroup"]) ? null : reader["VatGroup"].ToString(),
                                WebLineId = Convert.IsDBNull(reader["WebLineId"]) ? (long?)null : Convert.ToInt64(reader["WebLineId"]),
                                UdfDataJson = Convert.IsDBNull(reader["LineUdfData"]) ? null : reader["LineUdfData"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Gagal memuat Goods Receipt PO terpilih dari staging.", ex);
            }

            return result;
        }

        public List<PendingPurchaseOrder> LoadPendingStockTransfersByIds(IEnumerable<long> headerIds)
        {
            var result = new List<PendingPurchaseOrder>();
            if (string.IsNullOrEmpty(_connectionString) || headerIds == null) return result;

            var idList = new List<long>(headerIds);
            if (idList.Count == 0) return result;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string idsJoined = string.Join(",", idList);

                    string query = $@"
                        SELECT h.SOL_ID AS HeaderId, h.SOL_CARDCODE AS CardCode, h.SOL_CARDNAME AS CardName,
                               h.SOL_DOCDATE AS DocDate, h.SOL_DOCDUEDATE AS DocDueDate, h.SOL_TAXDATE AS TaxDate,
                               h.SOL_REMARKS AS Remarks, h.SOL_WEB_TX_NUMBER AS WebTxNumber, h.SOL_WEB_TX_ID AS WebTxId,
                               h.SOL_UDF_DATA AS HeaderUdfData,
                               d.SOL_LINENUM AS LineNum, d.SOL_ITEMCODE AS ItemCode, d.SOL_ITEMNAME AS ItemName,
                               d.SOL_FROM_WAREHOUSE AS FromWarehouse, d.SOL_WAREHOUSE AS Warehouse,
                               d.SOL_QUANTITY AS Quantity, d.SOL_PRICE AS Price,
                               d.SOL_WEB_LINE_ID AS WebLineId, d.SOL_UDF_DATA AS LineUdfData
                        FROM SOL_STOCK_TRANSFER_HEADER h
                        INNER JOIN SOL_STOCK_TRANSFER_DETAIL d ON d.SOL_HEADER_ID = h.SOL_ID
                        WHERE h.SOL_ID IN ({idsJoined}) AND h.SOL_PROCESS_STATUS IN (0, 2)
                        ORDER BY h.SOL_ID, d.SOL_LINENUM";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        PendingPurchaseOrder current = null;
                        long currentHeaderId = -1;

                        while (reader.Read())
                        {
                            long headerId = Convert.ToInt64(reader["HeaderId"]);

                            if (current == null || headerId != currentHeaderId)
                            {
                                current = new PendingPurchaseOrder
                                {
                                    HeaderId = headerId,
                                    CardCode = reader["CardCode"]?.ToString() ?? "",
                                    CardName = reader["CardName"]?.ToString() ?? "",
                                    DocDate = Convert.IsDBNull(reader["DocDate"]) ? DateTime.Now : Convert.ToDateTime(reader["DocDate"]),
                                    DocDueDate = Convert.IsDBNull(reader["DocDueDate"]) ? DateTime.Now.AddDays(7) : Convert.ToDateTime(reader["DocDueDate"]),
                                    TaxDate = Convert.IsDBNull(reader["TaxDate"]) ? DateTime.Now : Convert.ToDateTime(reader["TaxDate"]),
                                    Remarks = reader["Remarks"]?.ToString() ?? "",
                                    WebTxNumber = Convert.IsDBNull(reader["WebTxNumber"]) ? null : reader["WebTxNumber"].ToString(),
                                    WebTxId = Convert.IsDBNull(reader["WebTxId"]) ? (long?)null : Convert.ToInt64(reader["WebTxId"]),
                                    UdfDataJson = Convert.IsDBNull(reader["HeaderUdfData"]) ? null : reader["HeaderUdfData"].ToString()
                                };
                                result.Add(current);
                                currentHeaderId = headerId;
                            }

                            current.Lines.Add(new PendingPurchaseOrderLine
                            {
                                LineNum = Convert.IsDBNull(reader["LineNum"]) ? 0 : Convert.ToInt32(reader["LineNum"]),
                                ItemCode = reader["ItemCode"]?.ToString() ?? "",
                                ItemName = reader["ItemName"]?.ToString() ?? "",
                                FromWarehouse = reader["FromWarehouse"]?.ToString() ?? "",
                                Warehouse = reader["Warehouse"]?.ToString() ?? "",
                                Quantity = Convert.IsDBNull(reader["Quantity"]) ? 0 : Convert.ToDecimal(reader["Quantity"]),
                                Price = Convert.IsDBNull(reader["Price"]) ? 0 : Convert.ToDecimal(reader["Price"]),
                                WebLineId = Convert.IsDBNull(reader["WebLineId"]) ? (long?)null : Convert.ToInt64(reader["WebLineId"]),
                                UdfDataJson = Convert.IsDBNull(reader["LineUdfData"]) ? null : reader["LineUdfData"].ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Gagal memuat Stock Transfer terpilih dari staging.", ex);
            }

            return result;
        }

        #region DEBUG & MAINTENANCE
        public class TransactionSummary
        {
            public int PurchaseOrderHeaderCount { get; set; }
            public int PurchaseOrderDetailCount { get; set; }
            public int GoodsReceiptPOHeaderCount { get; set; }
            public int GoodsReceiptPODetailCount { get; set; }
            public int StockTransferHeaderCount { get; set; }
            public int StockTransferDetailCount { get; set; }
            public int SyncHistoryCount { get; set; }
            public int SyncErrorCount { get; set; }

            public int TotalTransactions
            {
                get { return PurchaseOrderHeaderCount + GoodsReceiptPOHeaderCount + StockTransferHeaderCount; }
            }

            public int TotalRecords
            {
                get
                {
                    return PurchaseOrderHeaderCount + PurchaseOrderDetailCount +
                           GoodsReceiptPOHeaderCount + GoodsReceiptPODetailCount +
                           StockTransferHeaderCount + StockTransferDetailCount +
                           SyncHistoryCount + SyncErrorCount;
                }
            }
        }

        public TransactionSummary GetTransactionSummary()
        {
            var summary = new TransactionSummary();
            if (string.IsNullOrEmpty(_connectionString)) return summary;

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string query = @"
                        SELECT
                            CASE WHEN OBJECT_ID('SOL_PURCHASE_ORDER_HEADER', 'U') IS NOT NULL THEN (SELECT COUNT(*) FROM SOL_PURCHASE_ORDER_HEADER) ELSE 0 END AS PO_H,
                            CASE WHEN OBJECT_ID('SOL_PURCHASE_ORDER_DETAIL', 'U') IS NOT NULL THEN (SELECT COUNT(*) FROM SOL_PURCHASE_ORDER_DETAIL) ELSE 0 END AS PO_D,
                            CASE WHEN OBJECT_ID('SOL_GRPO_HEADER', 'U') IS NOT NULL THEN (SELECT COUNT(*) FROM SOL_GRPO_HEADER) ELSE 0 END AS GRPO_H,
                            CASE WHEN OBJECT_ID('SOL_GRPO_DETAIL', 'U') IS NOT NULL THEN (SELECT COUNT(*) FROM SOL_GRPO_DETAIL) ELSE 0 END AS GRPO_D,
                            CASE WHEN OBJECT_ID('SOL_STOCK_TRANSFER_HEADER', 'U') IS NOT NULL THEN (SELECT COUNT(*) FROM SOL_STOCK_TRANSFER_HEADER) ELSE 0 END AS ST_H,
                            CASE WHEN OBJECT_ID('SOL_STOCK_TRANSFER_DETAIL', 'U') IS NOT NULL THEN (SELECT COUNT(*) FROM SOL_STOCK_TRANSFER_DETAIL) ELSE 0 END AS ST_D,
                            CASE WHEN OBJECT_ID('TBL_SYNC_HISTORY', 'U') IS NOT NULL THEN (SELECT COUNT(*) FROM TBL_SYNC_HISTORY) ELSE 0 END AS SYNC_H,
                            CASE WHEN OBJECT_ID('TBL_SYNC_ERROR', 'U') IS NOT NULL THEN (SELECT COUNT(*) FROM TBL_SYNC_ERROR) ELSE 0 END AS SYNC_E";

                    using (var cmd = new SqlCommand(query, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            summary.PurchaseOrderHeaderCount = Convert.ToInt32(reader["PO_H"]);
                            summary.PurchaseOrderDetailCount = Convert.ToInt32(reader["PO_D"]);
                            summary.GoodsReceiptPOHeaderCount = Convert.ToInt32(reader["GRPO_H"]);
                            summary.GoodsReceiptPODetailCount = Convert.ToInt32(reader["GRPO_D"]);
                            summary.StockTransferHeaderCount = Convert.ToInt32(reader["ST_H"]);
                            summary.StockTransferDetailCount = Convert.ToInt32(reader["ST_D"]);
                            summary.SyncHistoryCount = Convert.ToInt32(reader["SYNC_H"]);
                            summary.SyncErrorCount = Convert.ToInt32(reader["SYNC_E"]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetTransactionSummary Error: " + ex.Message);
            }

            return summary;
        }

        public bool DeleteAllTransactions(out int totalDeleted, out string errorMessage)
        {
            totalDeleted = 0;
            errorMessage = string.Empty;

            if (string.IsNullOrEmpty(_connectionString))
            {
                errorMessage = "Connection string database belum terkonfigurasi.";
                return false;
            }

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    var summaryBefore = GetTransactionSummary();
                    totalDeleted = summaryBefore.TotalRecords;

                    string deleteScript = @"
                        IF OBJECT_ID('SOL_PURCHASE_ORDER_DETAIL', 'U') IS NOT NULL
                        BEGIN
                            DELETE FROM SOL_PURCHASE_ORDER_DETAIL;
                            BEGIN TRY DBCC CHECKIDENT('SOL_PURCHASE_ORDER_DETAIL', RESEED, 0); END TRY BEGIN CATCH END CATCH;
                        END

                        IF OBJECT_ID('SOL_PURCHASE_ORDER_HEADER', 'U') IS NOT NULL
                        BEGIN
                            DELETE FROM SOL_PURCHASE_ORDER_HEADER;
                            BEGIN TRY DBCC CHECKIDENT('SOL_PURCHASE_ORDER_HEADER', RESEED, 0); END TRY BEGIN CATCH END CATCH;
                        END

                        IF OBJECT_ID('SOL_GRPO_DETAIL', 'U') IS NOT NULL
                        BEGIN
                            DELETE FROM SOL_GRPO_DETAIL;
                            BEGIN TRY DBCC CHECKIDENT('SOL_GRPO_DETAIL', RESEED, 0); END TRY BEGIN CATCH END CATCH;
                        END

                        IF OBJECT_ID('SOL_GRPO_HEADER', 'U') IS NOT NULL
                        BEGIN
                            DELETE FROM SOL_GRPO_HEADER;
                            BEGIN TRY DBCC CHECKIDENT('SOL_GRPO_HEADER', RESEED, 0); END TRY BEGIN CATCH END CATCH;
                        END

                        IF OBJECT_ID('SOL_STOCK_TRANSFER_DETAIL', 'U') IS NOT NULL
                        BEGIN
                            DELETE FROM SOL_STOCK_TRANSFER_DETAIL;
                            BEGIN TRY DBCC CHECKIDENT('SOL_STOCK_TRANSFER_DETAIL', RESEED, 0); END TRY BEGIN CATCH END CATCH;
                        END

                        IF OBJECT_ID('SOL_STOCK_TRANSFER_HEADER', 'U') IS NOT NULL
                        BEGIN
                            DELETE FROM SOL_STOCK_TRANSFER_HEADER;
                            BEGIN TRY DBCC CHECKIDENT('SOL_STOCK_TRANSFER_HEADER', RESEED, 0); END TRY BEGIN CATCH END CATCH;
                        END

                        IF OBJECT_ID('TBL_SYNC_HISTORY', 'U') IS NOT NULL
                        BEGIN
                            DELETE FROM TBL_SYNC_HISTORY;
                            BEGIN TRY DBCC CHECKIDENT('TBL_SYNC_HISTORY', RESEED, 0); END TRY BEGIN CATCH END CATCH;
                        END

                        IF OBJECT_ID('TBL_SYNC_ERROR', 'U') IS NOT NULL
                        BEGIN
                            DELETE FROM TBL_SYNC_ERROR;
                            BEGIN TRY DBCC CHECKIDENT('TBL_SYNC_ERROR', RESEED, 0); END TRY BEGIN CATCH END CATCH;
                        END";

                    using (var cmd = new SqlCommand(deleteScript, conn))
                    {
                        cmd.CommandTimeout = 120;
                        cmd.ExecuteNonQuery();
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        public bool DeleteSingleTransaction(string docType, long headerId, string webTxNumber, string docEntry, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (string.IsNullOrEmpty(_connectionString))
            {
                errorMessage = "Connection string database belum terkonfigurasi.";
                return false;
            }

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string sql = string.Empty;
                    if (string.Equals(docType, "Purchase Order", StringComparison.OrdinalIgnoreCase))
                    {
                        sql = @"
                            IF OBJECT_ID('SOL_PURCHASE_ORDER_DETAIL', 'U') IS NOT NULL
                                DELETE FROM SOL_PURCHASE_ORDER_DETAIL WHERE SOL_HEADER_ID = @HeaderId;
                            IF OBJECT_ID('SOL_PURCHASE_ORDER_HEADER', 'U') IS NOT NULL
                                DELETE FROM SOL_PURCHASE_ORDER_HEADER WHERE SOL_ID = @HeaderId;";
                    }
                    else if (string.Equals(docType, "Goods Receipt PO", StringComparison.OrdinalIgnoreCase))
                    {
                        sql = @"
                            IF OBJECT_ID('SOL_GRPO_DETAIL', 'U') IS NOT NULL
                                DELETE FROM SOL_GRPO_DETAIL WHERE SOL_HEADER_ID = @HeaderId;
                            IF OBJECT_ID('SOL_GRPO_HEADER', 'U') IS NOT NULL
                                DELETE FROM SOL_GRPO_HEADER WHERE SOL_ID = @HeaderId;";
                    }
                    else if (string.Equals(docType, "Stock Transfer", StringComparison.OrdinalIgnoreCase))
                    {
                        sql = @"
                            IF OBJECT_ID('SOL_STOCK_TRANSFER_DETAIL', 'U') IS NOT NULL
                                DELETE FROM SOL_STOCK_TRANSFER_DETAIL WHERE SOL_HEADER_ID = @HeaderId;
                            IF OBJECT_ID('SOL_STOCK_TRANSFER_HEADER', 'U') IS NOT NULL
                                DELETE FROM SOL_STOCK_TRANSFER_HEADER WHERE SOL_ID = @HeaderId;";
                    }
                    else
                    {
                        errorMessage = "Tipe dokumen tidak didukung: " + docType;
                        return false;
                    }

                    // Hapus juga catatan riwayat sync/error terkait transaksi ini jika ada nomor transaksi
                    sql += @"
                        IF OBJECT_ID('TBL_SYNC_HISTORY', 'U') IS NOT NULL
                        BEGIN
                            IF @DocEntry <> '' DELETE FROM TBL_SYNC_HISTORY WHERE DocType = @DocType AND DocEntry = @DocEntry;
                            IF @WebTxNumber <> '' DELETE FROM TBL_SYNC_HISTORY WHERE DocType = @DocType AND DocEntry = @WebTxNumber;
                        END

                        IF OBJECT_ID('TBL_SYNC_ERROR', 'U') IS NOT NULL
                        BEGIN
                            IF @DocEntry <> '' DELETE FROM TBL_SYNC_ERROR WHERE DocType = @DocType AND DocEntry = @DocEntry;
                            IF @WebTxNumber <> '' DELETE FROM TBL_SYNC_ERROR WHERE DocType = @DocType AND DocEntry = @WebTxNumber;
                        END";

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@HeaderId", headerId);
                        cmd.Parameters.AddWithValue("@DocType", docType ?? "");
                        cmd.Parameters.AddWithValue("@WebTxNumber", webTxNumber ?? "");
                        cmd.Parameters.AddWithValue("@DocEntry", docEntry ?? "");
                        cmd.ExecuteNonQuery();
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }
        #endregion

        #region Dead-Letter Queue & Reconciliation
        /// <summary>
        /// Mengembalikan dokumen yang berada di Dead-Letter Queue (status 3) kembali ke antrean pending (status 0).
        /// </summary>
        public int RequeueDeadLetterTransactions(string docType = null)
        {
            if (string.IsNullOrEmpty(_connectionString)) return 0;
            int totalRequeued = 0;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    bool doAll = string.IsNullOrEmpty(docType);
                    string dt = (docType ?? "").ToLower();

                    if (doAll || dt.Contains("purchase") || dt.Contains("po"))
                    {
                        string qPo = @"
                            UPDATE SOL_PURCHASE_ORDER_HEADER
                            SET SOL_PROCESS_STATUS = 0,
                                SOL_RETRYCOUNT = 0,
                                SOL_ERRORMESSAGE = NULL,
                                SOL_UPDATED_AT = GETDATE()
                            WHERE SOL_PROCESS_STATUS = 3;
                            
                            UPDATE d
                            SET d.SOL_PROCESS_STATUS = 0,
                                d.SOL_UPDATED_AT = GETDATE()
                            FROM SOL_PURCHASE_ORDER_DETAIL d
                            INNER JOIN SOL_PURCHASE_ORDER_HEADER h ON h.SOL_ID = d.SOL_HEADER_ID
                            WHERE h.SOL_PROCESS_STATUS = 0;";
                        using (SqlCommand cmd = new SqlCommand(qPo, conn))
                        {
                            totalRequeued += cmd.ExecuteNonQuery();
                        }
                    }

                    if (doAll || dt.Contains("goods") || dt.Contains("grpo"))
                    {
                        string qGrpo = @"
                            UPDATE SOL_GRPO_HEADER
                            SET SOL_PROCESS_STATUS = 0,
                                SOL_RETRYCOUNT = 0,
                                SOL_ERRORMESSAGE = NULL,
                                SOL_UPDATED_AT = GETDATE()
                            WHERE SOL_PROCESS_STATUS = 3;

                            UPDATE d
                            SET d.SOL_PROCESS_STATUS = 0,
                                d.SOL_UPDATED_AT = GETDATE()
                            FROM SOL_GRPO_DETAIL d
                            INNER JOIN SOL_GRPO_HEADER h ON h.SOL_ID = d.SOL_HEADER_ID
                            WHERE h.SOL_PROCESS_STATUS = 0;";
                        using (SqlCommand cmd = new SqlCommand(qGrpo, conn))
                        {
                            totalRequeued += cmd.ExecuteNonQuery();
                        }
                    }

                    if (doAll || dt.Contains("transfer") || dt.Contains("stock"))
                    {
                        string qTransfer = @"
                            UPDATE SOL_STOCK_TRANSFER_HEADER
                            SET SOL_PROCESS_STATUS = 0,
                                SOL_RETRYCOUNT = 0,
                                SOL_ERRORMESSAGE = NULL,
                                SOL_UPDATED_AT = GETDATE()
                            WHERE SOL_PROCESS_STATUS = 3;

                            UPDATE d
                            SET d.SOL_PROCESS_STATUS = 0,
                                d.SOL_UPDATED_AT = GETDATE()
                            FROM SOL_STOCK_TRANSFER_DETAIL d
                            INNER JOIN SOL_STOCK_TRANSFER_HEADER h ON h.SOL_ID = d.SOL_HEADER_ID
                            WHERE h.SOL_PROCESS_STATUS = 0;";
                        using (SqlCommand cmd = new SqlCommand(qTransfer, conn))
                        {
                            totalRequeued += cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("DB RequeueDeadLetterTransactions Error: " + ex.Message);
            }

            return totalRequeued;
        }

        /// <summary>
        /// Mengambil daftar dokumen sukses dari staging untuk dicek status akhirnya ke SAP B1 (Reconciliation).
        /// </summary>
        public List<Tuple<long, string, string>> LoadSyncedDocumentsForReconciliation(string docType, int limit = 50)
        {
            var result = new List<Tuple<long, string, string>>(); // HeaderId, DocEntry, WebTxNumber
            if (string.IsNullOrEmpty(_connectionString)) return result;

            string tableName = "SOL_PURCHASE_ORDER_HEADER";
            string type = (docType ?? "").ToLower();
            if (type.Contains("grpo") || type.Contains("goods")) tableName = "SOL_GRPO_HEADER";
            else if (type.Contains("transfer") || type.Contains("stock")) tableName = "SOL_STOCK_TRANSFER_HEADER";

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = $@"
                        SELECT TOP ({limit}) SOL_ID, SOL_DOCENTRY, SOL_WEB_TX_NUMBER
                        FROM {tableName}
                        WHERE SOL_PROCESS_STATUS = 1 
                          AND SOL_DOCENTRY IS NOT NULL 
                          AND (SOL_REMARKS NOT LIKE '%[RECONCILED]%' OR SOL_REMARKS IS NULL)
                        ORDER BY SOL_PROCESSED_AT DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            long id = Convert.ToInt64(reader["SOL_ID"]);
                            string docEntry = reader["SOL_DOCENTRY"]?.ToString();
                            string txNum = reader["SOL_WEB_TX_NUMBER"]?.ToString();
                            if (!string.IsNullOrEmpty(docEntry))
                            {
                                result.Add(Tuple.Create(id, docEntry, txNum));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("DB LoadSyncedDocumentsForReconciliation Error: " + ex.Message);
            }

            return result;
        }

        /// <summary>
        /// Menandai dokumen di staging bahwa status rekonsiliasi sudah di-update ke Web App.
        /// </summary>
        public void MarkDocumentAsReconciled(string docType, long headerId, string status)
        {
            if (string.IsNullOrEmpty(_connectionString)) return;

            string tableName = "SOL_PURCHASE_ORDER_HEADER";
            string type = (docType ?? "").ToLower();
            if (type.Contains("grpo") || type.Contains("goods")) tableName = "SOL_GRPO_HEADER";
            else if (type.Contains("transfer") || type.Contains("stock")) tableName = "SOL_STOCK_TRANSFER_HEADER";

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = $@"
                        UPDATE {tableName}
                        SET SOL_REMARKS = ISNULL(SOL_REMARKS, '') + ' [RECONCILED: ' + @Status + ']',
                            SOL_UPDATED_AT = GETDATE()
                        WHERE SOL_ID = @HeaderId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Status", status);
                        cmd.Parameters.AddWithValue("@HeaderId", headerId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("DB MarkDocumentAsReconciled Error: " + ex.Message);
            }
        }
        #endregion
    }
}

