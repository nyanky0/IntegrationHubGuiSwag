using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SAPbobsCOM;
using SOLTIUS_Scheduler_Add_On.Model;
using SOLTIUS_Scheduler_Add_On.UI;

namespace SOLTIUS_Scheduler_Add_On.Services
{
    public class SapSyncService : IDisposable
    {
        private Company _oCompany;

        public void ConnectToDIAPI(AppConfig config)
        {
            if (_oCompany == null) _oCompany = new Company();
            if (_oCompany.Connected) _oCompany.Disconnect();

            string serverType = (config.SAPServerType ?? "").ToUpper();
            if (serverType.Contains("2019")) _oCompany.DbServerType = BoDataServerTypes.dst_MSSQL2019;
            else if (serverType.Contains("2017")) _oCompany.DbServerType = BoDataServerTypes.dst_MSSQL2017;
            else if (serverType.Contains("2016")) _oCompany.DbServerType = BoDataServerTypes.dst_MSSQL2016;
            else if (serverType.Contains("2022")) _oCompany.DbServerType = BoDataServerTypes.dst_MSSQL2022;
            else if (serverType.Contains("HANA")) _oCompany.DbServerType = BoDataServerTypes.dst_HANADB;
            else _oCompany.DbServerType = BoDataServerTypes.dst_MSSQL2017;

            string serverName = config.SAPDBServer;
            if (string.IsNullOrWhiteSpace(serverName) || serverName.Equals("localhost", StringComparison.OrdinalIgnoreCase) || serverName == "127.0.0.1")
            {
                serverName = Environment.MachineName;
            }
            _oCompany.Server = serverName;

            string cleanLicense = (config.SAPLicenseServer ?? "")
                .Replace("https://", "").Replace("http://", "").Split('/')[0];

            string host = cleanLicense.Contains(":") ? cleanLicense.Split(':')[0] : cleanLicense;
            if (string.IsNullOrWhiteSpace(host) || host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || host == "127.0.0.1")
            {
                host = Environment.MachineName;
            }

            string port = cleanLicense.Contains(":") ? cleanLicense.Split(':')[1] : "40000";
            string sldPort = (port == "40002" || string.IsNullOrEmpty(port)) ? "40000" : port;

            _oCompany.SLDServer = $"{host}:{sldPort}";
            _oCompany.LicenseServer = $"{host}:{sldPort}";
            _oCompany.CompanyDB = config.SAPDatabase;
            _oCompany.UserName = config.SAPUser;
            _oCompany.Password = config.SAPPass;
            _oCompany.DbUserName = config.SAPDBUser;
            _oCompany.DbPassword = config.SAPDBPass;

            int retCode = _oCompany.Connect();
            if (retCode != 0)
            {
                _oCompany.GetLastError(out int errCode, out string errMsg);
                throw new Exception($"SAP Connection Failed [{errCode}]: {errMsg}");
            }
        }



        public void ExecutePurchaseOrderSync(SyncLogModel task)
        {
            Documents oOrder = null;
            Document_Lines oLines = null;

            try
            {
                oOrder = (Documents)_oCompany.GetBusinessObject(BoObjectTypes.oPurchaseOrders);
                oOrder.CardCode = task.CardCode;
                oOrder.DocDueDate = DateTime.Now.AddDays(7);
                oOrder.Comments = "Sync via SOLTIUS Scheduler";

                oLines = oOrder.Lines;
                oLines.ItemCode = task.ItemCode;
                oLines.Quantity = task.Quantity;
                oLines.Price = task.Price;
                string resolvedWhs = ResolveWarehouse(task.WarehouseCode);
                if (!string.IsNullOrEmpty(resolvedWhs))
                {
                    try { oLines.WarehouseCode = resolvedWhs; } catch { }
                }

                int addResult = oOrder.Add();
                if (addResult != 0)
                {
                    _oCompany.GetLastError(out int errCode, out string errMsg);
                    if (errMsg.Contains("[SQL Server]") || errMsg.Contains("ODBC") ||
                        errMsg.Contains("Native Client") || errMsg.Contains("SBO_SP_TransactionNotification"))
                    {
                        task.ErrorSource = "Custom Validation (SP)";
                        int lastBracket = errMsg.LastIndexOf(']');
                        if (lastBracket >= 0 && lastBracket < errMsg.Length - 1)
                            errMsg = errMsg.Substring(lastBracket + 1).Trim();
                    }
                    else
                    {
                        task.ErrorSource = "SAP Validation";
                    }

                    throw new Exception($"[{errCode}] {errMsg}");
                }

                task.DocEntry = _oCompany.GetNewObjectKey();
                task.Status = "Success";
                task.ErrorSource = "-";
                task.ErrorMessage = "-";
            }
            catch (System.Runtime.InteropServices.COMException comEx)
            {
                task.ErrorSource = "Server / Network";
                throw new Exception(comEx.Message);
            }
            catch (Exception ex)
            {
                if (string.IsNullOrEmpty(task.ErrorSource)) task.ErrorSource = "Application Logic";
                throw new Exception(ex.Message);
            }
            finally
            {
                if (oLines != null)
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(oLines);
                    oLines = null;
                }
                if (oOrder != null)
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(oOrder);
                    oOrder = null;
                }
            }
        }

        public void ExecuteGoodsReceiptPOSync(SyncLogModel task)
        {
            Documents oDelivery = null;
            Document_Lines oLines = null;

            try
            {
                oDelivery = (Documents)_oCompany.GetBusinessObject(BoObjectTypes.oPurchaseDeliveryNotes);
                oDelivery.CardCode = task.CardCode;
                oDelivery.DocDueDate = DateTime.Now.AddDays(7);
                oDelivery.Comments = "Sync via SOLTIUS Scheduler";

                oLines = oDelivery.Lines;
                oLines.ItemCode = task.ItemCode;
                oLines.Quantity = task.Quantity;
                oLines.Price = task.Price;
                string resolvedWhs = ResolveWarehouse(task.WarehouseCode);
                if (!string.IsNullOrEmpty(resolvedWhs))
                {
                    try { oLines.WarehouseCode = resolvedWhs; } catch { }
                }

                int addResult = oDelivery.Add();
                if (addResult != 0)
                {
                    _oCompany.GetLastError(out int errCode, out string errMsg);
                    if (errMsg.Contains("[SQL Server]") || errMsg.Contains("ODBC") ||
                        errMsg.Contains("Native Client") || errMsg.Contains("SBO_SP_TransactionNotification"))
                    {
                        task.ErrorSource = "Custom Validation (SP)";
                        int lastBracket = errMsg.LastIndexOf(']');
                        if (lastBracket >= 0 && lastBracket < errMsg.Length - 1)
                            errMsg = errMsg.Substring(lastBracket + 1).Trim();
                    }
                    else
                    {
                        task.ErrorSource = "SAP Validation";
                    }

                    throw new Exception($"[{errCode}] {errMsg}");
                }

                task.DocEntry = _oCompany.GetNewObjectKey();
                task.Status = "Success";
                task.ErrorSource = "-";
                task.ErrorMessage = "-";
            }
            catch (System.Runtime.InteropServices.COMException comEx)
            {
                task.ErrorSource = "Server / Network";
                throw new Exception(comEx.Message);
            }
            catch (Exception ex)
            {
                if (string.IsNullOrEmpty(task.ErrorSource)) task.ErrorSource = "Application Logic";
                throw new Exception(ex.Message);
            }
            finally
            {
                if (oLines != null)
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(oLines);
                    oLines = null;
                }
                if (oDelivery != null)
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(oDelivery);
                    oDelivery = null;
                }
            }
        }

        public void ExecuteStockTransferSync(SyncLogModel task)
        {
            StockTransfer oStock = null;
            StockTransfer_Lines oLines = null;

            try
            {
                oStock = (StockTransfer)_oCompany.GetBusinessObject(BoObjectTypes.oStockTransfer);
                oStock.DocDate = DateTime.Now;
                oStock.DueDate = DateTime.Now;
                oStock.TaxDate = DateTime.Now;
                oStock.Comments = "Sync via SOLTIUS Scheduler";

                string fromWhs = !string.IsNullOrEmpty(task.WarehouseCode) ? task.WarehouseCode : "WHS-TONGKOL";
                string resolvedWhs = ResolveWarehouse(fromWhs);
                if (!string.IsNullOrEmpty(resolvedWhs))
                {
                    try { oStock.FromWarehouse = resolvedWhs; } catch { }
                }

                oLines = oStock.Lines;
                oLines.ItemCode = task.ItemCode;
                oLines.Quantity = task.Quantity;

                int addResult = oStock.Add();
                if (addResult != 0)
                {
                    _oCompany.GetLastError(out int errCode, out string errMsg);
                    if (errMsg.Contains("[SQL Server]") || errMsg.Contains("ODBC") ||
                        errMsg.Contains("Native Client") || errMsg.Contains("SBO_SP_TransactionNotification"))
                    {
                        task.ErrorSource = "Custom Validation (SP)";
                        int lastBracket = errMsg.LastIndexOf(']');
                        if (lastBracket >= 0 && lastBracket < errMsg.Length - 1)
                            errMsg = errMsg.Substring(lastBracket + 1).Trim();
                    }
                    else
                    {
                        task.ErrorSource = "SAP Validation";
                    }

                    throw new Exception($"[{errCode}] {errMsg}");
                }

                task.DocEntry = _oCompany.GetNewObjectKey();
                task.Status = "Success";
                task.ErrorSource = "-";
                task.ErrorMessage = "-";
            }
            catch (System.Runtime.InteropServices.COMException comEx)
            {
                task.ErrorSource = "Server / Network";
                throw new Exception(comEx.Message);
            }
            catch (Exception ex)
            {
                if (string.IsNullOrEmpty(task.ErrorSource)) task.ErrorSource = "Application Logic";
                throw new Exception(ex.Message);
            }
            finally
            {
                if (oLines != null)
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(oLines);
                    oLines = null;
                }
                if (oStock != null)
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(oStock);
                    oStock = null;
                }
            }
        }

        /// <summary>
        /// Membuat Purchase Order multi-line di SAP dari data pending staging.
        /// Mengembalikan DocEntry SAP yang baru.
        /// </summary>
        public string ExecutePurchaseOrderSync(PendingPurchaseOrder order)
        {
            Documents oOrder = null;
            Document_Lines oLines = null;

            try
            {
                oOrder = (Documents)_oCompany.GetBusinessObject(BoObjectTypes.oPurchaseOrders);
                oOrder.CardCode = order.CardCode;
                oOrder.DocDate = order.DocDate;
                oOrder.DocDueDate = order.DocDueDate == DateTime.MinValue ? DateTime.Now.AddDays(7) : order.DocDueDate;
                oOrder.TaxDate = order.TaxDate;
                if (!string.IsNullOrEmpty(order.Remarks))
                    oOrder.Comments = order.Remarks;
                else
                    oOrder.Comments = "Sync via SOLTIUS Scheduler";

                // --- Header UDFs (U_SOL_...) ---
                if (!string.IsNullOrEmpty(order.WebTxNumber))
                    TrySetUserField(oOrder.UserFields, "U_SOL_WebTxNumber", order.WebTxNumber);
                if (order.WebTxId.HasValue)
                    TrySetUserField(oOrder.UserFields, "U_SOL_WebTxId", order.WebTxId.Value);

                ApplyDynamicUdfs(oOrder.UserFields, order.UdfDataJson);

                // --- Line Items & Line UDFs ---
                foreach (var line in order.Lines)
                {
                    oLines = oOrder.Lines;
                    oLines.ItemCode = line.ItemCode;
                    oLines.Quantity = (double)line.Quantity;
                    oLines.Price = (double)line.Price;
                    string resolvedWhs = ResolveWarehouse(line.Warehouse);
                    if (!string.IsNullOrEmpty(resolvedWhs))
                    {
                        try { oLines.WarehouseCode = resolvedWhs; } catch { }
                    }

                    string resolvedVat = ResolvePurchaseVatGroup(line.VatGroup);
                    if (!string.IsNullOrEmpty(resolvedVat))
                    {
                        try { oLines.VatGroup = resolvedVat; } catch { }
                    }

                    if (line.WebLineId.HasValue)
                        TrySetUserField(oLines.UserFields, "U_SOL_WebLineId", line.WebLineId.Value);

                    ApplyDynamicUdfs(oLines.UserFields, line.UdfDataJson);

                    oLines.Add();
                }

                int addResult = oOrder.Add();
                if (addResult != 0)
                {
                    _oCompany.GetLastError(out int errCode, out string errMsg);
                    throw new Exception($"[{errCode}] {errMsg}");
                }

                return _oCompany.GetNewObjectKey();
            }
            catch (System.Runtime.InteropServices.COMException comEx)
            {
                throw new Exception(comEx.Message);
            }
            finally
            {
                if (oLines != null)
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(oLines);
                    oLines = null;
                }
                if (oOrder != null)
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(oOrder);
                    oOrder = null;
                }
            }
        }

        /// <summary>
        /// Membuat Goods Receipt PO (GRPO) multi-line di SAP dari data pending staging.
        /// </summary>
        public string ExecuteGoodsReceiptPOSync(PendingPurchaseOrder grpo)
        {
            Documents oDelivery = null;
            Document_Lines oLines = null;

            try
            {
                oDelivery = (Documents)_oCompany.GetBusinessObject(BoObjectTypes.oPurchaseDeliveryNotes);
                oDelivery.CardCode = grpo.CardCode;
                oDelivery.DocDate = grpo.DocDate;
                oDelivery.DocDueDate = grpo.DocDueDate == DateTime.MinValue ? DateTime.Now.AddDays(7) : grpo.DocDueDate;
                oDelivery.TaxDate = grpo.TaxDate;
                if (!string.IsNullOrEmpty(grpo.Remarks))
                    oDelivery.Comments = grpo.Remarks;
                else
                    oDelivery.Comments = "GRPO Sync via SOLTIUS Scheduler";

                if (!string.IsNullOrEmpty(grpo.WebTxNumber))
                    TrySetUserField(oDelivery.UserFields, "U_SOL_WebTxNumber", grpo.WebTxNumber);
                if (grpo.WebTxId.HasValue)
                    TrySetUserField(oDelivery.UserFields, "U_SOL_WebTxId", grpo.WebTxId.Value);

                ApplyDynamicUdfs(oDelivery.UserFields, grpo.UdfDataJson);

                foreach (var line in grpo.Lines)
                {
                    oLines = oDelivery.Lines;
                    oLines.ItemCode = line.ItemCode;
                    oLines.Quantity = (double)line.Quantity;
                    oLines.Price = (double)line.Price;
                    string resolvedWhs = ResolveWarehouse(line.Warehouse);
                    if (!string.IsNullOrEmpty(resolvedWhs))
                    {
                        try { oLines.WarehouseCode = resolvedWhs; } catch { }
                    }

                    string resolvedVat = ResolvePurchaseVatGroup(line.VatGroup);
                    if (!string.IsNullOrEmpty(resolvedVat))
                    {
                        try { oLines.VatGroup = resolvedVat; } catch { }
                    }

                    if (line.WebLineId.HasValue)
                        TrySetUserField(oLines.UserFields, "U_SOL_WebLineId", line.WebLineId.Value);

                    ApplyDynamicUdfs(oLines.UserFields, line.UdfDataJson);

                    oLines.Add();
                }

                int addResult = oDelivery.Add();
                if (addResult != 0)
                {
                    _oCompany.GetLastError(out int errCode, out string errMsg);
                    throw new Exception($"[{errCode}] {errMsg}");
                }

                return _oCompany.GetNewObjectKey();
            }
            catch (System.Runtime.InteropServices.COMException comEx)
            {
                throw new Exception(comEx.Message);
            }
            finally
            {
                if (oLines != null)
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(oLines);
                    oLines = null;
                }
                if (oDelivery != null)
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(oDelivery);
                    oDelivery = null;
                }
            }
        }

        /// <summary>
        /// Membuat Inventory/Stock Transfer di SAP dari data pending staging.
        /// </summary>
        public string ExecuteStockTransferSync(PendingPurchaseOrder transfer)
        {
            StockTransfer oStock = null;
            StockTransfer_Lines oLines = null;

            try
            {
                oStock = (StockTransfer)_oCompany.GetBusinessObject(BoObjectTypes.oStockTransfer);
                oStock.DocDate = transfer.DocDate;
                oStock.DueDate = transfer.DocDueDate == DateTime.MinValue ? DateTime.Now : transfer.DocDueDate;
                oStock.TaxDate = transfer.TaxDate;
                if (!string.IsNullOrEmpty(transfer.Remarks))
                    oStock.Comments = transfer.Remarks;
                else
                    oStock.Comments = "Stock Transfer Sync via SOLTIUS Scheduler";

                if (!string.IsNullOrEmpty(transfer.WebTxNumber))
                    TrySetUserField(oStock.UserFields, "U_SOL_WebTxNumber", transfer.WebTxNumber);
                if (transfer.WebTxId.HasValue)
                    TrySetUserField(oStock.UserFields, "U_SOL_WebTxId", transfer.WebTxId.Value);

                ApplyDynamicUdfs(oStock.UserFields, transfer.UdfDataJson);

                foreach (var line in transfer.Lines)
                {
                    oLines = oStock.Lines;
                    oLines.ItemCode = line.ItemCode;
                    oLines.Quantity = (double)line.Quantity;

                    string fromWhs = !string.IsNullOrEmpty(line.Warehouse) ? line.Warehouse : "WHS-TONGKOL";
                    string resolvedWhs = ResolveWarehouse(fromWhs);
                    if (!string.IsNullOrEmpty(resolvedWhs))
                    {
                        try { oStock.FromWarehouse = resolvedWhs; } catch { }
                    }

                    if (line.WebLineId.HasValue)
                        TrySetUserField(oLines.UserFields, "U_SOL_WebLineId", line.WebLineId.Value);

                    ApplyDynamicUdfs(oLines.UserFields, line.UdfDataJson);

                    oLines.Add();
                }

                int addResult = oStock.Add();
                if (addResult != 0)
                {
                    _oCompany.GetLastError(out int errCode, out string errMsg);
                    throw new Exception($"[{errCode}] {errMsg}");
                }

                return _oCompany.GetNewObjectKey();
            }
            catch (System.Runtime.InteropServices.COMException comEx)
            {
                throw new Exception(comEx.Message);
            }
            finally
            {
                if (oLines != null)
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(oLines);
                    oLines = null;
                }
                if (oStock != null)
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(oStock);
                    oStock = null;
                }
            }
        }

        private static void TrySetUserField(UserFields userFields, string fieldName, object value)
        {
            if (userFields == null || string.IsNullOrEmpty(fieldName) || value == null) return;
            try
            {
                userFields.Fields.Item(fieldName).Value = value;
            }
            catch
            {
                // Lewati jika UDF belum dibuat di database SAP B1
            }
        }

        private static void ApplyDynamicUdfs(UserFields userFields, string udfJson)
        {
            if (string.IsNullOrWhiteSpace(udfJson) || userFields == null) return;
            try
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(udfJson);
                if (dict != null)
                {
                    foreach (var kvp in dict)
                    {
                        if (kvp.Value != null)
                        {
                            TrySetUserField(userFields, kvp.Key, kvp.Value);
                        }
                    }
                }
            }
            catch
            {
                // Abaikan kesalahan deserialisasi JSON jika data tidak sesuai
            }
        }

        private HashSet<string> _validWarehouses;
        private HashSet<string> _validInputTaxCodes;
        private HashSet<string> _validOutputTaxCodes;

        private void EnsureMasterDataCache()
        {
            if (_validWarehouses != null) return;
            _validWarehouses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _validInputTaxCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _validOutputTaxCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (_oCompany == null || !_oCompany.Connected) return;

            Recordset rs = null;
            try
            {
                rs = (Recordset)_oCompany.GetBusinessObject(BoObjectTypes.BoRecordset);
                rs.DoQuery("SELECT WhsCode FROM OWHS");
                while (!rs.EoF)
                {
                    var val = rs.Fields.Item(0).Value?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(val)) _validWarehouses.Add(val);
                    rs.MoveNext();
                }

                rs.DoQuery("SELECT Code, Category FROM OVTG");
                while (!rs.EoF)
                {
                    var code = rs.Fields.Item(0).Value?.ToString()?.Trim();
                    var cat = rs.Fields.Item(1).Value?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(code))
                    {
                        if (string.Equals(cat, "I", StringComparison.OrdinalIgnoreCase))
                            _validInputTaxCodes.Add(code);
                        else if (string.Equals(cat, "O", StringComparison.OrdinalIgnoreCase))
                            _validOutputTaxCodes.Add(code);
                    }
                    rs.MoveNext();
                }
            }
            catch
            {
                // Fallback jika query master data gagal
            }
            finally
            {
                if (rs != null)
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
                }
            }
        }

        private string ResolveWarehouse(string inputWarehouse)
        {
            if (string.IsNullOrWhiteSpace(inputWarehouse)) return null;

            string whs = inputWarehouse.Trim();
            if (whs.Equals("WHS-D1", StringComparison.OrdinalIgnoreCase)) whs = "WH-D1";

            EnsureMasterDataCache();

            if (_validWarehouses != null && _validWarehouses.Count > 0)
            {
                if (_validWarehouses.Contains(whs)) return whs;
                if (_validWarehouses.Contains("DC")) return "DC";
                if (_validWarehouses.Contains("WH-D1")) return "WH-D1";
                return null;
            }

            return whs;
        }

        private string ResolvePurchaseVatGroup(string inputVat)
        {
            if (string.IsNullOrWhiteSpace(inputVat)) return null;

            string vat = inputVat.Trim();
            if (vat.Equals("PPN11", StringComparison.OrdinalIgnoreCase) ||
                vat.Equals("PPN", StringComparison.OrdinalIgnoreCase) ||
                vat.Equals("11%", StringComparison.OrdinalIgnoreCase))
            {
                vat = "PPNM";
            }

            EnsureMasterDataCache();

            if (_validInputTaxCodes != null && _validInputTaxCodes.Count > 0)
            {
                if (_validInputTaxCodes.Contains(vat)) return vat;
                return null;
            }

            return vat;
        }

        private string ResolveSalesVatGroup(string inputVat)
        {
            if (string.IsNullOrWhiteSpace(inputVat)) return null;

            string vat = inputVat.Trim();
            if (vat.Equals("PPN11", StringComparison.OrdinalIgnoreCase) ||
                vat.Equals("PPN", StringComparison.OrdinalIgnoreCase) ||
                vat.Equals("11%", StringComparison.OrdinalIgnoreCase))
            {
                vat = "PPNK";
            }

            EnsureMasterDataCache();

            if (_validOutputTaxCodes != null && _validOutputTaxCodes.Count > 0)
            {
                if (_validOutputTaxCodes.Contains(vat)) return vat;
                return null;
            }

            return vat;
        }

        /// <summary>
        /// Mengecek status dokumen di SAP B1 (Open, Closed, Canceled) via Recordset.
        /// Kompatibel dengan SQL Server dan SAP HANA.
        /// </summary>
        public SapDocumentStatusResult GetDocumentStatus(string docType, string docEntry)
        {
            if (_oCompany == null || !_oCompany.Connected)
                throw new InvalidOperationException("SAP B1 DI-API belum terhubung.");

            Recordset oRecordset = null;
            try
            {
                bool isHana = _oCompany.DbServerType == BoDataServerTypes.dst_HANADB;
                string query = SapQueryHelper.GetDocumentStatusQuery(docType, docEntry, isHana);

                oRecordset = (Recordset)_oCompany.GetBusinessObject(BoObjectTypes.BoRecordset);
                oRecordset.DoQuery(query);

                if (oRecordset.RecordCount > 0)
                {
                    string docStatus = oRecordset.Fields.Item("DocStatus").Value?.ToString();
                    string canceled = oRecordset.Fields.Item("CANCELED").Value?.ToString();
                    string docNum = oRecordset.Fields.Item("DocNum").Value?.ToString();

                    string resolvedStatus = "Open";
                    if (string.Equals(canceled, "Y", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(canceled, "C", StringComparison.OrdinalIgnoreCase))
                    {
                        resolvedStatus = "Canceled";
                    }
                    else if (string.Equals(docStatus, "C", StringComparison.OrdinalIgnoreCase))
                    {
                        resolvedStatus = "Closed";
                    }

                    return new SapDocumentStatusResult
                    {
                        DocEntry = docEntry,
                        DocNum = docNum,
                        Status = resolvedStatus,
                        Exists = true
                    };
                }

                return new SapDocumentStatusResult
                {
                    DocEntry = docEntry,
                    Exists = false,
                    Status = "NotFound"
                };
            }
            finally
            {
                if (oRecordset != null)
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(oRecordset);
                    oRecordset = null;
                }
            }
        }

        public void Dispose()
        {
            if (_oCompany != null)
            {
                if (_oCompany.Connected) _oCompany.Disconnect();
                System.Runtime.InteropServices.Marshal.ReleaseComObject(_oCompany);
                _oCompany = null;
            }
        }
    }

    public class SapDocumentStatusResult
    {
        public string DocEntry { get; set; }
        public string DocNum { get; set; }
        public string Status { get; set; } // "Open", "Closed", "Canceled", "NotFound"
        public bool Exists { get; set; }
    }
}