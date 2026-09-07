using SOLTIUS_Web_API_Add_On.Models.Transaction;
using SOLTIUS_Web_API_Add_On.Repositories;

namespace SOLTIUS_Web_API_Add_On.Services
{
    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly IPurchaseOrderRepository _repository;

        public PurchaseOrderService(IPurchaseOrderRepository repository)
        {
            _repository = repository;
        }

        public async Task SavePurchaseOrderAsync(PurchaseOrderHeader purchaseOrder)
        {
            if (purchaseOrder == null)
                throw new ArgumentNullException(nameof(purchaseOrder));

            if (string.IsNullOrWhiteSpace(purchaseOrder.CardCode))
                throw new Exception("CardCode (Vendor Code) is required.");

            if (purchaseOrder.DocumentLines == null || purchaseOrder.DocumentLines.Count == 0)
                throw new Exception("DocumentLines is required.");

            await _repository.InsertPurchaseOrderAsync(purchaseOrder);
        }
    }
}
