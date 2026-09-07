using System;

namespace SOLTIUS_Scheduler_Add_On.Model
{
    public class PendingQueueDocModel
    {
        public bool IsSelected { get; set; } = true;
        public long HeaderId { get; set; }
        public string DocType { get; set; }
        public string WebTxNumber { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public DateTime DocDate { get; set; }
        public DateTime DocDueDate { get; set; }
        public int ProcessStatus { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Remarks { get; set; }
        public string UdfDataJson { get; set; }
    }
}
