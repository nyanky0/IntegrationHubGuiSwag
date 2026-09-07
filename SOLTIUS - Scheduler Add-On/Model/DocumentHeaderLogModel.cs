using System;

namespace SOLTIUS_Scheduler_Add_On.Model
{
    public class DocumentHeaderLogModel
    {
        public long HeaderId { get; set; }
        public string DocType { get; set; }
        public string WebTxNumber { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public DateTime DocDate { get; set; }
        public DateTime DocDueDate { get; set; }
        public string Status { get; set; }
        public string DocEntry { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Remarks { get; set; }
        public string ErrorMessage { get; set; }
        public string UdfDataJson { get; set; }
    }

    public class DocumentDetailLineModel
    {
        public int LineNum { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string Warehouse { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal LineTotal => Quantity * Price;
        public string VatGroup { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
        public string UdfDataJson { get; set; }
    }
}
