using SOLTIUS_Web_API_Add_On.Models.Transaction;

namespace SOLTIUS_Web_API_Add_On.Services
{
    public interface IPurchaseOrderService
    {
        Task SavePurchaseOrderAsync(PurchaseOrderHeader purchaseOrder);
    }
}
