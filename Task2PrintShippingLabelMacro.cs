using LinnworksAPI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinnworksMacro
{
    public class LinnworksMacro : LinnworksMacroHelpers.LinnworksMacroBase
    {
        private const string PRINTED_ORDERS_FOLDER = "Printed Orders";

        public void Execute()
        {
            var folders = Api.Orders.GetAvailableFolders();
            var printedOrdersFolder = folders.FirstOrDefault(f => f.FolderName.Equals(PRINTED_ORDERS_FOLDER, StringComparison.OrdinalIgnoreCase));

            if (printedOrdersFolder == null)
            {
                return;
            }

            var ordersToProcess = Api.Orders.GetAllOpenOrders(null, null, Guid.Empty, string.Empty);

            foreach (var orderId in ordersToProcess)
            {
                ProcessOrder(orderId, printedOrdersFolder.FolderName);
            }
        }

        private void ProcessOrder(Guid orderId, string folderName)
        {
            try
            {
                bool labelPrinted = PrintShippingLabel(orderId);

                if (labelPrinted)
                {
                    AssignOrderToFolder(orderId, folderName);
                }
            }
            catch (Exception)
            {
            }
        }

        private bool PrintShippingLabel(Guid orderId)
        {
            try
            {
                var templateType = "Shipping Labels";
                var parameters = new List<KeyValueRequest<string, string>>();

                var result = Api.PrintService.CreatePDFfromJobForceTemplate(
                    templateType,
                    new List<Guid> { orderId },
                    null,
                    parameters,
                    null,
                    "",
                    0,
                    null,
                    null
                );

                if (result == null || string.IsNullOrEmpty(result.URL))
                {
                    return false;
                }

                try
                {
                    bool isInternalNote = false;
                    var noteId = Api.ProcessedOrders.AddOrderNote(
                        orderId,
                        $"Shipping label URL: {result.URL}",
                        isInternalNote
                    );
                }
                catch (Exception)
                {
                }

                var labelsPrinted = Api.Orders.SetLabelsPrinted(new List<Guid> { orderId });

                if (labelsPrinted == null || !labelsPrinted.Contains(orderId))
                {
                    return false;
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void AssignOrderToFolder(Guid orderId, string folderName)
        {
            try
            {
                Api.Orders.AssignToFolder(new List<Guid> { orderId }, folderName);
            }
            catch (Exception)
            {
            }
        }
    }
}