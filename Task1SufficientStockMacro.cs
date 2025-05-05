using LinnworksAPI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinnworksMacro
{
    public class LinnworksMacro : LinnworksMacroHelpers.LinnworksMacroBase
    {
        private const string FC_LOCATION_NAME = "FC Location";

        public void Execute(Guid[] OrderIds)
        {
            var locations = GetAllLocations();
            var fcLocation = locations.FirstOrDefault(l => l.LocationName.Equals(FC_LOCATION_NAME, StringComparison.OrdinalIgnoreCase));

            if (fcLocation == null)
            {
                return;
            }

            if (OrderIds != null && OrderIds.Length > 0)
            {
                foreach (var orderId in OrderIds)
                {
                    ProcessOrder(orderId, fcLocation.StockLocationId);
                }
            }
            else
            {
                ProcessAllPaidOrders(fcLocation.StockLocationId);
            }
        }

        private List<StockLocation> GetAllLocations()
        {
            try
            {
                return Api.Inventory.GetStockLocations();
            }
            catch (Exception)
            {
                return new List<StockLocation>();
            }
        }

        private void ProcessAllPaidOrders(Guid fcLocationId)
        {
            try
            {
                var filter = new FieldsFilter
                {
                    ListFields = new List<ListFieldFilter>
                    {
                        new ListFieldFilter
                        {
                            FieldCode = FieldCode.GENERAL_INFO_STATUS,
                            Type = ListFieldFilterType.Is,
                            Value = "1"
                        }
                    }
                };

                var paidOrderIds = Api.Orders.GetAllOpenOrders(
                    filters: filter,
                    sorting: default,
                    fulfilmentCenter: default,
                    additionalFilter: string.Empty
                );

                if (paidOrderIds.Count == 0)
                {
                    return;
                }

                foreach (var orderId in paidOrderIds)
                {
                    ProcessOrderWithResult(orderId, fcLocationId);
                }
            }
            catch (Exception)
            {
            }
        }

        private bool ProcessOrderWithResult(Guid orderId, Guid fcLocationId)
        {
            try
            {
                var order = Api.Orders.GetOrderById(orderId);
                if (order == null)
                {
                    return false;
                }

                var userLocationId = Api.Orders.GetUserLocationId();
                if (order.FulfilmentLocationId != userLocationId)
                {
                    return false;
                }

                if (order.Items == null || order.Items.Count == 0)
                {
                    return false;
                }

                bool canFulfill = CanFulfillOrder(order.Items, fcLocationId);

                if (canFulfill)
                {
                    MoveOrderToLocation(orderId, fcLocationId);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void ProcessOrder(Guid orderId, Guid fcLocationId)
        {
            ProcessOrderWithResult(orderId, fcLocationId);
        }

        private bool CanFulfillOrder(List<OrderItem> items, Guid fcLocationId)
        {
            var stockItemIds = items.Where(i => !i.IsService)
                                   .Select(i => i.StockItemId)
                                   .Distinct()
                                   .ToList();

            if (stockItemIds.Count == 0)
            {
                return true;
            }

            try
            {
                var request = new GetStockLevel_BatchRequest
                {
                    StockItemIds = stockItemIds
                };

                var stockLevels = Api.Stock.GetStockLevel_Batch(request);

                if (stockLevels == null)
                {
                    return false;
                }

                foreach (var item in items)
                {
                    if (item.IsService)
                        continue;

                    var stockLevel = stockLevels
                        .FirstOrDefault(s => s.pkStockItemId == item.StockItemId);

                    if (stockLevel == null)
                    {
                        return false;
                    }

                    var fcStockLevel = stockLevel.StockItemLevels
                        .FirstOrDefault(l => l.Location.StockLocationId == fcLocationId);

                    if (fcStockLevel == null || fcStockLevel.Available < item.Quantity)
                    {
                        return false;
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void MoveOrderToLocation(Guid orderId, Guid locationId)
        {
            try
            {
                Api.Orders.MoveToLocation(new List<Guid> { orderId }, locationId);
            }
            catch (Exception)
            {
            }
        }
    }
}