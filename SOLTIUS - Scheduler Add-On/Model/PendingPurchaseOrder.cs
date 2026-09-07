using System;
using System.Collections.Generic;

namespace SOLTIUS_Scheduler_Add_On.Model
{
    /// <summary>
    /// Purchase Order pending dari tabel staging (SOL_PURCHASE_ORDER_HEADER + SOL_PURCHASE_ORDER_DETAIL)
    /// yang belum diproses (SOL_PROCESS_STATUS = 0).
    /// </summary>
    public class PendingPurchaseOrder
    {
        public long HeaderId { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public DateTime DocDate { get; set; }
        public DateTime DocDueDate { get; set; }
        public DateTime TaxDate { get; set; }
        public string Remarks { get; set; }
        public string WebTxNumber { get; set; }
        public long? WebTxId { get; set; }
        public string UdfDataJson { get; set; }
        public List<PendingPurchaseOrderLine> Lines { get; set; } = new List<PendingPurchaseOrderLine>();
    }

    public class PendingPurchaseOrderLine
    {
        public int LineNum { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string Warehouse { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public string VatGroup { get; set; }
        public long? WebLineId { get; set; }
        public string UdfDataJson { get; set; }
    }
}
