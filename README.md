# Application Macros and Inventory Integration Guide

This guide outlines how to set up SKUs, create system integration applications, and implement macros for inventory and order management using the developer portal.

---

## Table of Contents

1. [Getting Started](#getting-started)
2. [Developer Portal Overview](#developer-portal-overview)
3. [Application Setup](#application-setup)
4. [Macro Descriptions](#macro-descriptions)
    - [1. Sufficient Stock Allocation Macro](#1-sufficient-stock-allocation-macro)
    - [2. Print Shipping Label Macro](#2-print-shipping-label-macro)
    - [3. Query Data Sales Report for Supplier](#3-query-data-sales-report-for-supplier)
    - [4. Folder Assignment and Order Item Addition Macro](#4-folder-assignment-and-order-item-addition-macro)
    - [5. Purchase Order (PO) Creation Macro](#5-purchase-order-po-creation-macro)
    - [6. Order Identification Macro](#6-order-identification-macro)
    - [7. Custom Export](#7-custom-export)

---

## Getting Started

Before starting any tasks, it is recommended to create SKUs in the inventory system to ensure smooth operation of macros and integrations.

---

## Developer Portal Overview

In the **Developer Portal**, you can create:

- **Applications**: Preferably of type **System Integration**.
- **Macros**: Types include:
  - Rule Engine Order
  - API
  - Scheduled

Each application will have:
- `ID`
- `Secret`
- `Installation URL`

> **Note**: Installation is required to bind the application to a user account. This is essential for using the application token in SDKs or external systems to access live data with correct permissions.

---

## Application Setup

To configure an application for macro functionality:

1. Create a **System Integration** type application.
2. Add the `MacroModule`.
3. Assign necessary permissions.
4. Add desired macros from the list below.

---

## Macro Descriptions

### 1. Sufficient Stock Allocation Macro

- Navigate to `Inventory -> Location` and create a new **FC location**.
- Use `Settings -> Import Data` to upload SKU data via CSV.
- After the macro is executed:
  - Paid orders with available stock at the FC location will automatically allocate stock there.

---

### 2. Print Shipping Label Macro

- This macro prints shipping labels for orders.
- When run:
  - A label file is generated and can be viewed for printing.
  - In scheduled mode, order status changes to "Printed" and the label URL is saved in order notes.
- Limitation:
  - The actual image or print preview isn't directly embedded or retrievable in this mode.

---

### 3. Query Data Sales Report for Supplier

- Go to `Dashboard -> Query Data`.
- Create a custom **T-SQL script**.
- Use a `SupplierID` parameter to filter results.
- The script accesses data from this DB structure:
  ![DB Structure](https://docs.linnworks.com/resources/Storage/documentation/Files/LW_Database_structure_image.png)

---

### 4. Folder Assignment and Order Item Addition Macro

- Go to `Settings -> Order Management` and create a **"Possible Merge Orders"** folder.
- Add test orders with:
  - Identical delivery addresses
  - Extended properties including attribute type, name, and SKU value
- Orders meeting the criteria will be grouped or updated accordingly.

---

### 5. Purchase Order (PO) Creation Macro

- Assign suppliers and default suppliers to SKUs in the **Inventory**.
- This macro:
  - Detects when stock is insufficient
  - Automatically creates purchase orders
  - Groups items from multiple orders into one PO if they share the same default supplier

---

### 6. Order Identification Macro

- Use `Apps -> MacroConfiguration` to define configurations.
- Example:
  - Define a SKU
  - All orders containing that SKU will receive:
    - A unique order identifier
    - A note
    - An extended property

---

### 7. Custom Export

- Navigate to `Settings -> Export Data`.
- Configure:
  - Custom SQL query (T-SQL)
  - Output file location (e.g., Dropbox)
  - Column mapping
  - Schedule and timezone
- The export retrieves:
  - SKU
  - Item Product ID
  - Quantity
- Limitation:
  - Full image URL cannot be retrieved since only `fkImageId` is stored and does not include the full image path.

---

## Final Notes

- Ensure permissions and configurations are correctly set.
- Use the installation token for secure SDK or API integration.
- Refer to the [Linnworks database schema](https://docs.linnworks.com/resources/Storage/documentation/Files/LW_Database_structure_image.png) when writing custom SQL.

---
