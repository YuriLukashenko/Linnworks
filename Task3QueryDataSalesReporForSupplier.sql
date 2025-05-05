SELECT
	si.ItemNumber 				AS[SKU],
	pc.CategoryName             AS [Item Category],  
	ps.SupplierCode 			AS[Supplier Code],
	sl.Quantity                 AS [Stock Level],
	(sl.Quantity - sl.OnOrder)  AS[Available],
	sl.MinimumLevel 			AS[Min Level],
	sloc.Location             	AS [Location(Bin / Rack)],
	s.SupplierName 				AS[Supplier],
	ps.KnownPurchasePrice     	AS [Purchase Price],
	si.RetailPrice 				AS[Retail Price],
	COALESCE(sales.Sales30, 0) 	AS [Sales30Days],
	COALESCE(sales.Sales60, 0) 	AS[Sales60Days],
	COALESCE(sales.Sales90, 0) 	AS[Sales90Days]
FROM StockItem si
    INNER JOIN ItemSupplier ps
    ON ps.fkStockItemId = si.pkStockItemId
	AND ps.fkSupplierId  = @SupplierID
	AND ps.IsDefault     = 1
INNER JOIN Supplier s
	ON s.pkSupplierId = ps.fkSupplierId
LEFT JOIN StockLevel sl
	ON sl.fkStockItemId = si.pkStockItemId
	AND sl.fkStockLocationId = (
		SELECT pkStockLocationId
		FROM StockLocation
	WHERE Location = 'Default'
    )
LEFT JOIN StockLocation sloc
	ON sloc.pkStockLocationId = sl.fkStockLocationId

LEFT JOIN PackageGroups pg
	ON pg.PackageCategoryID = si.PackageGroup
LEFT JOIN ProductCategories pc
	ON pc.CategoryID = si.CategoryID

LEFT JOIN (
    SELECT
        oi.fkStockItemId,
		SUM(CASE WHEN o.dReceievedDate >= DATEADD(DAY, -30, GETDATE()) THEN oi.nQty ELSE 0 END) AS Sales30,
		SUM(CASE WHEN o.dReceievedDate >= DATEADD(DAY, -60, GETDATE()) THEN oi.nQty ELSE 0 END) AS Sales60,
		SUM(CASE WHEN o.dReceievedDate >= DATEADD(DAY, -90, GETDATE()) THEN oi.nQty ELSE 0 END) AS Sales90
	FROM dbo.Open_Order o
	JOIN dbo.Open_OrderItem oi
		ON oi.fkOrderID = o.pkOrderID
	WHERE o.bProcessed = 1
	GROUP BY oi.fkStockItemId
) sales
    ON sales.fkStockItemId = si.pkStockItemId

WHERE
	si.bLogicalDelete = 0
ORDER BY
	si.ItemNumber;