using LinnworksAPI;

namespace LinnworksMacro
{
    public class LinnworksMacro : LinnworksMacroHelpers.LinnworksMacroBase
    {
        private static readonly IEnumerable<string> EXTENDED_PROPERTY_NAMES = new List<string> { "AdditionalItem1", "AdditionalItem2" };

        public void Execute(Guid[] OrderIds)
        {
            var incomingOrdersInfo = Api.Orders.GetOrders(OrderIds.ToList(), null, false, true);
            var allOrderIds = GetAllOpenOrders();
            var comparableOrdersInfo = Api.Orders.GetOrders(allOrderIds, null, false, true);

            foreach (var orderId in OrderIds)
            {
                var orderDetails = incomingOrdersInfo.FirstOrDefault(o => o.OrderId == orderId);
                var matchedPropertyValues = GetMatchedPropertiesValues(orderId, EXTENDED_PROPERTY_NAMES);
                var productsIds = GetProductsBySkus(matchedPropertyValues);
                AddProductsToOrder(productsIds, orderId);

                foreach (var comparableOrderId in allOrderIds.Where(id => id != orderId))
                {
                    if (CompareOrderAddresses(orderDetails, comparableOrdersInfo.FirstOrDefault(o => o.OrderId == comparableOrderId)))
                    {
                        Api.Orders.AssignToFolder(new List<Guid> { orderId, comparableOrderId }, "Possible Merge Orders");
                    }
                }
            }
        }

        private List<string> GetMatchedPropertiesValues(Guid orderId, IEnumerable<string> extendedPropertyNames)
        {
            var orderExtendedProperties = Api.Orders.GetExtendedProperties(orderId);
            var matchedExtendedPropertyValues = new List<string>();

            foreach (var property in orderExtendedProperties)
            {
                if (extendedPropertyNames.Contains(property.Name))
                {
                    matchedExtendedPropertyValues.Add(property.Value);
                }
            }

            return matchedExtendedPropertyValues;
        }

        private List<Guid> GetProductsBySkus(List<string> skus)
        {
            try
            {
                if (skus.Count == 0)
                {
                    return new List<Guid>();
                }

                var stockItems = Api.Inventory.GetStockItemIdsBySKU(new GetStockItemIdsBySKURequest
                {
                    SKUS = skus
                }).Items;

                var stockItemIds = stockItems.Select(s => s.StockItemId).ToList();
                return stockItemIds;
            }
            catch (Exception)
            {
                return new List<Guid>();
            }

        }

        private void AddProductsToOrder(List<Guid> productsIds, Guid orderId)
        {
            foreach (var product in productsIds)
            {
                Api.Orders.AddOrderItem(orderId, product, string.Empty, Guid.Empty, 1, default);
            }
        }

        public List<Guid> GetAllOpenOrders()
        {
            return Api.Orders.GetAllOpenOrders(default, default, default, string.Empty);
        }

        private bool CompareOrderAddresses(OpenOrder baseOrder, OpenOrder comparableOrder)
        {
            if (!baseOrder.CustomerInfo.Address.FullName.ToLower()
                .Equals(comparableOrder.CustomerInfo.Address.FullName.ToLower()))
                return false;
            if (!baseOrder.CustomerInfo.Address.Address1.ToLower()
                .Equals(comparableOrder.CustomerInfo.Address.Address1.ToLower()))
                return false;
            if (!baseOrder.CustomerInfo.Address.Address2.ToLower()
                .Equals(comparableOrder.CustomerInfo.Address.Address2.ToLower()))
                return false;
            if (!baseOrder.CustomerInfo.Address.Address3.ToLower()
                .Equals(comparableOrder.CustomerInfo.Address.Address3.ToLower()))
                return false;
            if (!baseOrder.CustomerInfo.Address.PostCode.ToLower()
                .Equals(comparableOrder.CustomerInfo.Address.PostCode.ToLower()))
                return false;
            if (!baseOrder.CustomerInfo.Address.Country.ToLower()
                .Equals(comparableOrder.CustomerInfo.Address.Country.ToLower()))
                return false;
            if (!baseOrder.CustomerInfo.Address.Town.ToLower()
                .Equals(comparableOrder.CustomerInfo.Address.Town.ToLower()))
                return false;

            return true;
        }
    }
}
