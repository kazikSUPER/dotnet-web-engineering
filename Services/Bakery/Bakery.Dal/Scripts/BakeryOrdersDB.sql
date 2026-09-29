-- =============================================
-- Database: BakeryOrdersDB
-- Craft Bakery Order Management Subsystem
-- Technologies: MS SQL Server / LocalDB
-- =============================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'BakeryOrdersDB')
BEGIN
    CREATE DATABASE BakeryOrdersDB;
END
GO

USE BakeryOrdersDB;
GO

-- 1. Drop existing tables if needed (in reverse dependency order)
IF OBJECT_ID('dbo.OrderStatusHistory', 'U') IS NOT NULL DROP TABLE dbo.OrderStatusHistory;
IF OBJECT_ID('dbo.Payments', 'U') IS NOT NULL DROP TABLE dbo.Payments;
IF OBJECT_ID('dbo.OrderItems', 'U') IS NOT NULL DROP TABLE dbo.OrderItems;
IF OBJECT_ID('dbo.Orders', 'U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID('dbo.Customers', 'U') IS NOT NULL DROP TABLE dbo.Customers;
GO

-- 2. Create Customers Table
CREATE TABLE dbo.Customers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(150) NOT NULL,
    Phone NVARCHAR(30) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    DiscountPercent DECIMAL(5,2) NOT NULL CONSTRAINT DF_Customers_Discount DEFAULT 0.00,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT SYSUTCDATETIME()
);
GO

-- 3. Create Orders Table
CREATE TABLE dbo.Orders (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL,
    OrderDate DATETIME2 NOT NULL CONSTRAINT DF_Orders_OrderDate DEFAULT SYSUTCDATETIME(),
    Status NVARCHAR(50) NOT NULL CONSTRAINT DF_Orders_Status DEFAULT 'Pending',
    TotalAmount DECIMAL(18,2) NOT NULL,
    SpecialInstructions NVARCHAR(500) NULL,
    CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerId) 
        REFERENCES dbo.Customers(Id) ON DELETE CASCADE
);
GO

-- 4. Create OrderItems Table
CREATE TABLE dbo.OrderItems (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    ItemName NVARCHAR(150) NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    Quantity INT NOT NULL CONSTRAINT CK_OrderItems_Quantity CHECK (Quantity > 0),
    CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) 
        REFERENCES dbo.Orders(Id) ON DELETE CASCADE
);
GO

-- 5. Create Payments Table
CREATE TABLE dbo.Payments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    PaymentMethod NVARCHAR(50) NOT NULL,
    PaymentDate DATETIME2 NOT NULL CONSTRAINT DF_Payments_PaymentDate DEFAULT SYSUTCDATETIME(),
    TransactionReference NVARCHAR(100) NOT NULL,
    Status NVARCHAR(50) NOT NULL CONSTRAINT DF_Payments_Status DEFAULT 'Completed',
    CONSTRAINT FK_Payments_Orders FOREIGN KEY (OrderId) 
        REFERENCES dbo.Orders(Id) ON DELETE CASCADE
);
GO

-- 6. Create OrderStatusHistory Table
CREATE TABLE dbo.OrderStatusHistory (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    ChangedAt DATETIME2 NOT NULL CONSTRAINT DF_OrderStatusHistory_ChangedAt DEFAULT SYSUTCDATETIME(),
    Comment NVARCHAR(250) NULL,
    CONSTRAINT FK_OrderStatusHistory_Orders FOREIGN KEY (OrderId) 
        REFERENCES dbo.Orders(Id) ON DELETE CASCADE
);
GO

-- 7. Performance Indexes
CREATE NONCLUSTERED INDEX IX_Orders_CustomerId ON dbo.Orders(CustomerId);
CREATE NONCLUSTERED INDEX IX_OrderItems_OrderId ON dbo.OrderItems(OrderId);
CREATE NONCLUSTERED INDEX IX_Payments_OrderId ON dbo.Payments(OrderId);
CREATE NONCLUSTERED INDEX IX_OrderStatusHistory_OrderId ON dbo.OrderStatusHistory(OrderId);
GO

-- 8. Stored Procedure: Get Customer Order Summary (Dapper Stored Procedure Requirement)
IF OBJECT_ID('dbo.sp_GetCustomerOrderSummary', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetCustomerOrderSummary;
GO

CREATE PROCEDURE dbo.sp_GetCustomerOrderSummary
    @CustomerId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        c.Id AS CustomerId,
        c.FullName,
        c.Phone,
        c.Email,
        c.DiscountPercent,
        COUNT(o.Id) AS TotalOrders,
        ISNULL(SUM(o.TotalAmount), 0.00) AS TotalSpent,
        MAX(o.OrderDate) AS LastOrderDate
    FROM dbo.Customers c
    LEFT JOIN dbo.Orders o ON c.Id = o.CustomerId
    WHERE c.Id = @CustomerId
    GROUP BY c.Id, c.FullName, c.Phone, c.Email, c.DiscountPercent;
END
GO

-- 9. Seed Initial Data
INSERT INTO dbo.Customers (FullName, Phone, Email, DiscountPercent)
VALUES 
    (N'Оксана Шевченко', N'+380501234567', N'oksana.sh@example.com', 5.00),
    (N'Тарас Мельник', N'+380679876543', N'taras.m@example.com', 0.00),
    (N'Ірина Бондар', N'+380931112233', N'irina.b@example.com', 10.00);

-- Orders for Customer 1
INSERT INTO dbo.Orders (CustomerId, OrderDate, Status, TotalAmount, SpecialInstructions)
VALUES (1, SYSUTCDATETIME(), N'Ready', 245.00, N'Без нарізки');

DECLARE @Order1Id INT = SCOPE_IDENTITY();

INSERT INTO dbo.OrderItems (OrderId, ItemName, UnitPrice, Quantity)
VALUES 
    (@Order1Id, N'Крафтовий подовий хліб на заквасці', 65.00, 2),
    (@Order1Id, N'Французький мигдалевий круасан', 57.50, 2);

INSERT INTO dbo.Payments (OrderId, Amount, PaymentMethod, TransactionReference, Status)
VALUES (@Order1Id, 245.00, N'Card', N'TXN-2026-00192', N'Completed');

INSERT INTO dbo.OrderStatusHistory (OrderId, Status, Comment)
VALUES 
    (@Order1Id, N'Pending', N'Замовлення оформлено онлайн'),
    (@Order1Id, N'Baking', N'Хліб у печі'),
    (@Order1Id, N'Ready', N'Готово до видачі у пекарні');

-- Orders for Customer 2
INSERT INTO dbo.Orders (CustomerId, OrderDate, Status, TotalAmount, SpecialInstructions)
VALUES (2, SYSUTCDATETIME(), N'Baking', 130.00, N'Добре пропечений багет');

DECLARE @Order2Id INT = SCOPE_IDENTITY();

INSERT INTO dbo.OrderItems (OrderId, ItemName, UnitPrice, Quantity)
VALUES 
    (@Order2Id, N'Традиційний французький багет', 45.00, 2),
    (@Order2Id, N'Чіабата з оливками', 40.00, 1);

INSERT INTO dbo.OrderStatusHistory (OrderId, Status, Comment)
VALUES 
    (@Order2Id, N'Pending', N'Замовлення прийнято'),
    (@Order2Id, N'Baking', N'Передано пекарю');
GO
