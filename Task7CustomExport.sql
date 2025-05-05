SELECT
    oi.ItemNumber AS [Product_SKU],
    (SELECT TOP 1 ooi.fkImageId 
        FROM [Open_OrderItem] ooi
        WHERE ooi.ItemNumber = oi.ItemNumber) AS [Product_Image_Id],
    SUM(oi.nQty) AS [Quantity]

FROM
	[OrderItem] oi
INNER JOIN [Order] o
	ON oi.fkOrderID = o.pkOrderID

WHERE o.bProcessed = 1
AND
	o.dProcessedOn BETWEEN
DATEADD(hour, 15, DATEADD(day, -1, CAST(CAST(GETDATE() AS DATE) AS DATETIME))) AND
DATEADD(hour, 15, CAST(CAST(GETDATE() AS DATE) AS DATETIME))

GROUP BY oi.ItemNumber;