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
        /// Mengambil Sales Order pending dari tabel staging (process_status = 0),
        /// termasuk detail barisnya, untuk disinkronkan ke SAP.
        /// </summary>
        public List<PendingSalesOrder> LoadPendingSalesOrders()
        {
            var result = new List<PendingSalesOrder>();
            if (string.IsNullOrEmpty(_connectionString)) return result;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string query = @"
                        SELECT h.id AS HeaderId, h.cardcode, h.cardname, h.docdate, h.docduedate, h.taxdate, h.remarks,
                               d.linenum, d.itemcode, d.itemname, d.warehouse, d.quantity, d.price
                        FROM sales_order_header h
                        INNER JOIN sales_order_detail d ON d.header_id = h.id
                        WHERE h.process_status = 0
                        ORDER BY h.id, d.linenum";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        PendingSalesOrder current = null;
                        long currentHeaderId = -1;

                        while (reader.Read())
                        {
                            long headerId = Convert.ToInt64(reader["HeaderId"]);

                            if (current == null || headerId != currentHeaderId)
                            {
                                current = new PendingSalesOrder
                                {
                                    HeaderId = headerId,
                                    CardCode = reader["cardcode"]?.ToString() ?? "",
                                    CardName = reader["cardname"]?.ToString() ?? "",
                                    DocDate = Convert.IsDBNull(reader["docdate"]) ? DateTime.Now : Convert.ToDateTime(reader["docdate"]),
                                    DocDueDate = Convert.IsDBNull(reader["docduedate"]) ? DateTime.Now.AddDays(7) : Convert.ToDateTime(reader["docduedate"]),
                                    TaxDate = Convert.IsDBNull(reader["taxdate"]) ? DateTime.Now : Convert.ToDateTime(reader["taxdate"]),
                                    Remarks = reader["remarks"]?.ToString() ?? ""
                                };
                                result.Add(current);
                                currentHeaderId = headerId;
                            }

                            current.Lines.Add(new PendingSalesOrderLine
                            {
                                LineNum = Convert.IsDBNull(reader["linenum"]) ? 0 : Convert.ToInt32(reader["linenum"]),
                                ItemCode = reader["itemcode"]?.ToString() ?? "",
                                ItemName = reader["itemname"]?.ToString() ?? "",
                                Warehouse = reader["warehouse"]?.ToString() ?? "",
                                Quantity = Convert.IsDBNull(reader["quantity"]) ? 0 : Convert.ToDecimal(reader["quantity"]),
                                Price = Convert.IsDBNull(reader["price"]) ? 0 : Convert.ToDecimal(reader["price"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Gagal memuat Sales Order pending dari staging.", ex);
            }

            return result;
        }

        /// <summary>
        /// Update status SO pending di tabel staging setelah proses sync ke SAP.
        /// process_status: 0 = pending, 1 = sukses, 2 = gagal.
        /// retrycount hanya bertambah saat gagal; di-reset saat sukses.
        /// </summary>
        public void UpdateSalesOrderStatus(long headerId, int processStatus, string errorMessage = null)
        {
            if (string.IsNullOrEmpty(_connectionString)) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string query = @"
                        UPDATE sales_order_header
                        SET process_status = @Status,
                            errormessage = @ErrMsg,
                            retrycount = CASE WHEN @Status = 1 THEN 0 ELSE retrycount + 1 END,
                            processed_at = GETDATE(),
                            updated_at = GETDATE()
                        WHERE id = @HeaderId;

                        UPDATE sales_order_detail
                        SET process_status = @Status,
                            updated_at = GETDATE()
                        WHERE header_id = @HeaderId;";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@HeaderId", headerId);
                        cmd.Parameters.AddWithValue("@Status", processStatus);
                        cmd.Parameters.AddWithValue("@ErrMsg", (object)errorMessage ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("DB Update Status Error: " + ex.Message);
            }
        }

        /// <summary>
        /// Returns true if the SO header has exceeded the max retry limit.
        /// </summary>
        public bool IsRetryLimitExceeded(long headerId)
        {
            if (string.IsNullOrEmpty(_connectionString)) return false;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"SELECT retrycount FROM sales_order_header WHERE id = @HeaderId";
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
        /// Mark SO as dead-letter (status = 3) when retry limit exceeded.
        /// </summary>
        public void MarkAsExceededRetryLimit(long headerId)
        {
            if (string.IsNullOrEmpty(_connectionString)) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"
                        UPDATE sales_order_header
                        SET process_status = 3,
                            errormessage = 'Max retry limit exceeded',
                            updated_at = GETDATE()
                        WHERE id = @HeaderId";

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
                        WHERE h.SOL_PROCESS_STATUS = 0
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
        /// SOL_PROCESS_STATUS: 0 = pending, 1 = sukses, 2 = gagal.
        /// retrycount hanya bertambah saat gagal; di-reset saat sukses.
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
                        SET SOL_PROCESS_STATUS = @Status,
                            SOL_ERRORMESSAGE = @ErrMsg,
                            SOL_DOCENTRY = CASE WHEN @DocEntry IS NOT NULL THEN @DocEntry ELSE SOL_DOCENTRY END,
                            SOL_RETRYCOUNT = CASE WHEN @Status = 1 THEN 0 ELSE SOL_RETRYCOUNT + 1 END,
                            SOL_PROCESSED_AT = GETDATE(),
                            SOL_UPDATED_AT = GETDATE()
                        WHERE SOL_ID = @HeaderId;

                        UPDATE SOL_PURCHASE_ORDER_DETAIL
                        SET SOL_PROCESS_STATUS = @Status,
                            SOL_UPDATED_AT = GETDATE()
                        WHERE SOL_HEADER_ID = @HeaderId;";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@HeaderId", headerId);
                        cmd.Parameters.AddWithValue("@Status", processStatus);
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

                    // Query Sales Orders
                    try
                    {
                        string querySO = @"
                            SELECT id AS HeaderId, 'Sales Order' AS DocType,
                                   '' AS WebTxNumber,
                                   ISNULL(cardcode, '') AS CardCode,
                                   ISNULL(cardname, '') AS CardName,
                                   docdate AS DocDate,
                                   docduedate AS DocDueDate,
                                   process_status AS ProcessStatus,
                                   '' AS DocEntry,
                                   processed_at AS ProcessedAt,
                                   created_at AS CreatedAt,
                                   ISNULL(remarks, '') AS Remarks,
                                   ISNULL(errormessage, '') AS ErrorMessage,
                                   NULL AS UdfDataJson
                            FROM sales_order_header";

                        using (SqlCommand cmd = new SqlCommand(querySO, conn))
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
                    catch (Exception exSO)
                    {
                        Console.WriteLine("LoadDocumentHeaderLogs SO Error: " + exSO.Message);
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

        public List<DocumentDetailLineModel> LoadSalesOrderLineDetails(long headerId)
        {
            var lines = new List<DocumentDetailLineModel>();
            if (string.IsNullOrEmpty(_connectionString)) return lines;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT linenum AS LineNum,
                               ISNULL(itemcode, '') AS ItemCode,
                               ISNULL(itemname, '') AS ItemName,
                               ISNULL(warehouse, '') AS Warehouse,
                               ISNULL(quantity, 0) AS Quantity,
                               ISNULL(price, 0) AS Price,
                               '' AS VatGroup,
                               process_status AS ProcessStatus,
                               ISNULL(errormessage, '') AS ErrorMessage,
                               NULL AS UdfDataJson
                        FROM sales_order_detail
                        WHERE header_id = @HeaderId
                        ORDER BY linenum";

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
                                else if (statusVal == 3) statusStr = "Dead-Letter";

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
                Console.WriteLine("LoadSalesOrderLineDetails Error: " + ex.Message);
            }

            return lines;
        }
    }
}
