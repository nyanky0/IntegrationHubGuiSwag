using System;
using SAPbobsCOM;

namespace SOLTIUS_Scheduler_Add_On.Services
{
    /// <summary>
    /// Helper sentral otomatisasi dialek SQL Server vs SAP HANA untuk SAP B1.
    /// Memastikan query yang dijalankan via Recordset (DI-API) maupun direct staging
    /// berjalan 100% kompatibel baik pada instance MSSQL maupun SAP HANA.
    /// </summary>
    public static class SapQueryHelper
    {
        public static bool IsHana(BoDataServerTypes dbServerType)
        {
            return dbServerType == BoDataServerTypes.dst_HANADB;
        }

        public static bool IsHana(string serverType)
        {
            if (string.IsNullOrEmpty(serverType)) return false;
            return serverType.IndexOf("HANA", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Query Master Items (OITM) yang kompatibel SQL Server & SAP HANA.
        /// </summary>
        public static string GetItemsQuery(int limit = 100, bool isHana = false)
        {
            if (isHana)
            {
                return @"SELECT ""ItemCode"", ""ItemName"", ""ItmsGrpCod"" AS ""GroupCode"", 
                                ""InvntryUom"" AS ""UomCode"", ""SalUnitMsr"" AS ""SalesUom"",
                                ""UpdateDate"", ""CreateDate""
                         FROM ""OITM""
                         ORDER BY ""ItemCode"" LIMIT " + limit;
            }
            else
            {
                return @"SELECT TOP (" + limit + @") 
                                ItemCode, ItemName, ItmsGrpCod as GroupCode, 
                                InvntryUom as UomCode, SalUnitMsr as SalesUom,
                                UpdateDate, CreateDate
                         FROM OITM
                         ORDER BY ItemCode";
            }
        }

        /// <summary>
        /// Query Master Business Partners / Vendors (OCRD) yang kompatibel SQL Server & SAP HANA.
        /// </summary>
        public static string GetBusinessPartnersQuery(int limit = 200, bool isHana = false)
        {
            if (isHana)
            {
                return @"SELECT ""CardCode"", ""CardName"", ""CardType"", ""GroupCode"", ""Currency"", ""ValidFor""
                         FROM ""OCRD""
                         WHERE ""CardType"" = 'S'
                         ORDER BY ""CardName"" LIMIT " + limit;
            }
            else
            {
                return @"SELECT TOP (" + limit + @") 
                                CardCode, CardName, CardType, GroupCode, Currency, ValidFor
                         FROM OCRD
                         WHERE CardType = 'S'
                         ORDER BY CardName";
            }
        }

        /// <summary>
        /// Query Master Warehouses (OWHS) yang kompatibel SQL Server & SAP HANA.
        /// </summary>
        public static string GetWarehousesQuery(bool isHana = false)
        {
            return isHana
                ? @"SELECT ""WhsCode"", ""WhsName"" FROM ""OWHS"" WHERE ""Locked"" = 'N' ORDER BY ""WhsCode"""
                : @"SELECT WhsCode, WhsName FROM OWHS WHERE Locked = 'N' ORDER BY WhsCode";
        }

        /// <summary>
        /// Query Master Tax Codes (OVTG).
        /// </summary>
        public static string GetTaxCodesQuery(bool isHana = false)
        {
            return isHana
                ? @"SELECT ""Code"", ""Category"" FROM ""OVTG"""
                : @"SELECT Code, Category FROM OVTG";
        }

        /// <summary>
        /// Format fungsi waktu saat ini (GETDATE() vs CURRENT_TIMESTAMP).
        /// </summary>
        public static string GetCurrentTimestamp(bool isHana = false)
        {
            return isHana ? "CURRENT_TIMESTAMP" : "GETDATE()";
        }

        /// <summary>
        /// Query status dokumen SAP B1 (OPOR, OPDN, OWTR) untuk Two-Way Status Reconciliation.
        /// Mengembalikan DocEntry, DocNum, DocStatus ('O'='Open', 'C'='Closed'), dan CANCELED ('Y'/'N').
        /// </summary>
        public static string GetDocumentStatusQuery(string docType, string docEntry, bool isHana = false)
        {
            string tableName = "OPOR";
            string type = (docType ?? "").Trim().ToLower();

            if (type.Contains("grpo") || type.Contains("goods receipt") || type.Contains("delivery"))
                tableName = "OPDN";
            else if (type.Contains("transfer") || type.Contains("stock") || type.Contains("packing"))
                tableName = "OWTR";

            long safeDocEntry = 0;
            long.TryParse(docEntry, out safeDocEntry);

            if (isHana)
            {
                return $@"SELECT ""DocEntry"", ""DocNum"", ""DocStatus"", ""CANCELED"" 
                         FROM ""{tableName}"" 
                         WHERE ""DocEntry"" = {safeDocEntry}";
            }
            else
            {
                return $@"SELECT DocEntry, DocNum, DocStatus, CANCELED 
                         FROM {tableName} 
                         WHERE DocEntry = {safeDocEntry}";
            }
        }
    }
}

