using LinnworksAPI;

namespace LinnworksMacro
{
    public class LinnworksMacro : LinnworksMacroHelpers.LinnworksMacroBase
    {
        public void Execute(Guid[] OrderIds)
        {
            var defaultLocation = Api.Locations.GetLocation(default);

            var purchaseOrderItems = GetPurchaseOrderItems(OrderIds, defaultLocation.StockLocationId);
            CreatePurchaseOrders(purchaseOrderItems, defaultLocation.StockLocationId);
        }

        private IEnumerable<PurchaseOrderItem> GetPurchaseOrderItems(Guid[] orderIds, Guid location)
        {
            var purchaseOrderItemsList = new List<PurchaseOrderItem>();

            foreach (var order in orderIds)
            {
                var orderItems = Api.Orders.GetOrderItems(order, location);
                var orderSuppliers = Api.Orders.GetOpenOrderItemsSuppliers(order);
                foreach (var item in orderItems)
                {
                    if (item.AvailableStock >= 0)
                    {
                        continue;
                    }

                    if (!purchaseOrderItemsList.Any(poi => poi.StockItemId == item.StockItemId))
                    {
                        purchaseOrderItemsList.Add(new PurchaseOrderItem
                        {
                            StockItemId = item.StockItemId,
                            SupplierId = orderSuppliers.FirstOrDefault(s => s.Key == item.StockItemId).Value,
                            RequiredQuantity = item.Level - item.Quantity,
                            Cost = (item.UnitCost * item.Quantity) + (item.UnitCost * item.TaxRate / 100) //link to calc
                        });
                    }
                    else
                    {
                        var existingItem = purchaseOrderItemsList.First(poi => poi.StockItemId == item.StockItemId);
                        existingItem.RequiredQuantity -= item.Quantity;
                        existingItem.Cost += (item.UnitCost * item.Quantity) + (item.UnitCost * item.TaxRate / 100);
                    }
                }
            }
            return UpdatePurchaseOrderItemsQuantity(purchaseOrderItemsList);
        }

        private void CreatePurchaseOrders(IEnumerable<PurchaseOrderItem> purchaseOrderItems, Guid location)
        {
            if (purchaseOrderItems.Count() == 0)
            {
                return;
            }

            foreach (var supplier in purchaseOrderItems.Select(poi => poi.SupplierId).Distinct())
            {
                var purchaseOrder = Api.PurchaseOrder.Create_PurchaseOrder_Initial(new Create_PurchaseOrder_InitialParameter
                {
                    fkSupplierId = supplier,
                    fkLocationId = location,
                    ExternalInvoiceNumber = $"EI No.{new Random().Next(10000, 99999)}",
                    Currency = "GBP",
                    SupplierReferenceNumber = $"SR No.{new Random().Next(10000, 99999)}",
                    UnitAmountTaxIncludedType = 2,
                    DateOfPurchase = DateTime.UtcNow,
                    QuotedDeliveryDate = DateTime.UtcNow.AddDays(3),
                    ConversionRate = 1
                });

                foreach (var purchaseOrderItem in purchaseOrderItems.Where(poi => poi.SupplierId == supplier))
                {
                    Api.PurchaseOrder.Add_PurchaseOrderItem(new Add_PurchaseOrderItemParameter
                    {
                        pkPurchaseId = purchaseOrder,
                        fkStockItemId = purchaseOrderItem.StockItemId,
                        Qty = purchaseOrderItem.RequiredQuantity,
                        Cost = (decimal)purchaseOrderItem.Cost,
                        TaxRate = 0,
                        PackQuantity = 1,
                        PackSize = 1
                    });
                }
            }
        }

        private IEnumerable<PurchaseOrderItem> UpdatePurchaseOrderItemsQuantity(IEnumerable<PurchaseOrderItem> purchaseOrderItems)
        {
            foreach (var item in purchaseOrderItems)
            {
                if (item.RequiredQuantity >= 0)
                {
                    purchaseOrderItems = purchaseOrderItems.Where(poi => poi.StockItemId != item.StockItemId);
                    continue;
                }
                item.RequiredQuantity *= -1;
            }
            return purchaseOrderItems;
        }

        public class PurchaseOrderItem
        {
            public Guid StockItemId { get; set; }
            public Guid SupplierId { get; set; }
            public int RequiredQuantity { get; set; }
            public double Cost { get; set; }
        }
    }
}
