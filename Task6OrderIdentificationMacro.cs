using LinnworksAPI;

namespace LinnworksMacro
{
    public class LinnworksMacro : LinnworksMacroHelpers.LinnworksMacroBase
    {
        public const string IDENTIDIER_TAG = "CUSTOM IDENTIFIER #1";
        public const string EXTENDED_PROPERTY_NAME = "EXTENDED PROPERTY BY SKU";
        public const string EXTENDED_PROPERTY_TYPE = "AdditionalInfo";

        public void Execute(Guid[] OrderIds, string skuParameter)
        {
            var ordersInfo = Api.Orders.GetOrders(OrderIds.ToList(), null, false, true);
            var matchedOrders = new List<OpenOrder>();

            foreach (var order in ordersInfo)
            {
                var orderItems = Api.Orders.GetOrderItems(order.OrderId, default);
                foreach (var orderItem in orderItems)
                {
                    if (orderItem.SKU != skuParameter)
                    {
                        continue;
                    }

                    if (!matchedOrders.Contains(order))
                    {
                        matchedOrders.Add(order);
                    }
                    SetOrderNote(order, orderItems.Count(x => x.SKU == skuParameter));
                }
            }
            SetOrderIdentifier(matchedOrders.Select(x => x.OrderId).ToArray(), IDENTIDIER_TAG);
            SetOrderExtendedProperty(matchedOrders.Select(x => x.OrderId).ToArray(), skuParameter);
        }

        private void SetOrderIdentifier(Guid[] orderIds, string tag)
        {
            var request = new ChangeOrderIdentifierRequest
            {
                OrderIds = orderIds,
                Tag = tag
            };
            Api.OpenOrders.AssignOrderIdentifier(request);
        }

        private void SetOrderExtendedProperty(Guid[] orderIds, string propertyValue)
        {
            var extendedProperty = new BasicExtendedProperty()
            {
                Name = EXTENDED_PROPERTY_NAME,
                Value = propertyValue,
                Type = EXTENDED_PROPERTY_TYPE
            };

            foreach (var order in orderIds)
            {
                var request = new AddExtendedPropertiesRequest()
                {
                    ExtendedProperties = new[] { extendedProperty },
                    OrderId = order
                };
                Api.Orders.AddExtendedProperties(request);
            }
        }

        private void SetOrderNote(OpenOrder orderInfo, int matchedSkuItemsCount)
        {
            var orderNote = new OrderNote()
            {
                OrderId = orderInfo.OrderId,
                Note = $"Name: {orderInfo.CustomerInfo.Address.FullName}. Count SKU-parameter items: {matchedSkuItemsCount}",
                Internal = true
            };
            Api.Orders.SetOrderNotes(orderInfo.OrderId, new List<OrderNote> { orderNote });
        }
    }
}
