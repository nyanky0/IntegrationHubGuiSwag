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

        public void ExecuteSalesOrderSync(SyncLogModel task)
        {
            Documents oOrder = null;
            Document_Lines oLines = null; // Tambahkan variabel eksplisit untuk Lines

            try
            {
                oOrder = (Documents)_oCompany.GetBusinessObject(BoObjectTypes.oOrders);
                oOrder.CardCode = task.CardCode;
                oOrder.DocDueDate = DateTime.Now.AddDays(7);
                oOrder.Comments = "Sync via SOLTIUS Scheduler";

                // Gunakan variabel eksplisit, jangan di-chain (oOrder.Lines.xxx)
                oLines = oOrder.Lines;
                oLines.ItemCode = task.ItemCode;
                oLines.Quantity = task.Quantity;
                oLines.Price = task.Price;
                oLines.WarehouseCode = task.WarehouseCode;

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
                // Selalu rilis COM Object dari child ke parent
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
        /// Membuat Sales Order multi-line di SAP dari data pending staging.
        /// Mengembalikan DocEntry SAP yang baru.
        /// </summary>
        public string ExecuteSalesOrderSync(PendingSalesOrder order)
        {
            Documents oOrder = null;
            Document_Lines oLines = null;

            try
            {
                oOrder = (Documents)_oCompany.GetBusinessObject(BoObjectTypes.oOrders);
                oOrder.CardCode = order.CardCode;
                oOrder.DocDate = order.DocDate;
                oOrder.DocDueDate = order.DocDueDate == DateTime.MinValue ? DateTime.Now.AddDays(7) : order.DocDueDate;
                oOrder.TaxDate = order.TaxDate;
                if (!string.IsNullOrEmpty(order.Remarks))
                    oOrder.Comments = order.Remarks;
                else
                    oOrder.Comments = "Sync via SOLTIUS Scheduler";

                foreach (var line in order.Lines)
                {
                    oLines = oOrder.Lines;
                    oLines.ItemCode = line.ItemCode;
                    oLines.Quantity = (double)line.Quantity;
                    oLines.Price = (double)line.Price;
                    if (!string.IsNullOrEmpty(line.Warehouse))
                        oLines.WarehouseCode = line.Warehouse;
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
                oLines.WarehouseCode = task.WarehouseCode;

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
                    if (!string.IsNullOrEmpty(line.Warehouse))
                        oLines.WarehouseCode = line.Warehouse;
                    if (!string.IsNullOrEmpty(line.VatGroup))
                        oLines.VatGroup = line.VatGroup;

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
}