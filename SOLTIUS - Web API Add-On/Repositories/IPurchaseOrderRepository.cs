using SOLTIUS_Web_API_Add_On.Models.Transaction;

namespace SOLTIUS_Web_API_Add_On.Repositories
{
    public class StagingPoStatus
    {
        public long Id { get; set; }
        public string WebTxNumber { get; set; }
        public byte ProcessStatus { get; set; }
        public string DocEntry { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public interface IPurchaseOrderRepository
    {
        Task InsertPurchaseOrderAsync(PurchaseOrderHeader purchaseOrder);
        Task<StagingPoStatus> GetStatusByWebTxNumberAsync(string webTxNumber);
        Task<IEnumerable<StagingPoStatus>> GetSyncStatusListAsync(DateTime? since);
        Task<bool> DeleteFromStagingAsync(string webTxNumber);
        Task<bool> RequestCancelInSapAsync(string webTxNumber, string sapDocNum, string reason);
    }
}
