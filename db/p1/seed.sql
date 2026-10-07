-- ============================================================================
-- ПРОЄКТ №1: Ідемпотентне початкове наповнення даними (Seed Data)
-- Стек: SQL Server (T-SQL)
-- ============================================================================

SET NOCOUNT ON;

-- 1. Клієнти (перевірка за Email)
IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE Email = 'olena.koval@example.com')
BEGIN
    INSERT INTO dbo.Customers (FirstName, LastName, Email, PhoneNumber, Gender, Age, CreatedBy)
    VALUES (N'Олена', N'Коваль', 'olena.koval@example.com', '0501112233', 'F', 28, 'Seeder');
END

IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE Email = 'taras.shevchuk@example.com')
BEGIN
    INSERT INTO dbo.Customers (FirstName, LastName, Email, PhoneNumber, Gender, Age, CreatedBy)
    VALUES (N'Тарас', N'Шевчук', 'taras.shevchuk@example.com', '0673334455', 'M', 34, 'Seeder');
END

IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE Email = 'iryna.melnyk@example.com')
BEGIN
    INSERT INTO dbo.Customers (FirstName, LastName, Email, PhoneNumber, Gender, Age, CreatedBy)
    VALUES (N'Ірина', N'Мельник', 'iryna.melnyk@example.com', '0937778899', 'F', 24, 'Seeder');
END

IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE Email = 'bohdan.tkachuk@example.com')
BEGIN
    INSERT INTO dbo.Customers (FirstName, LastName, Email, PhoneNumber, Gender, Age, CreatedBy)
    VALUES (N'Богдан', N'Ткачук', 'bohdan.tkachuk@example.com', '0684445566', 'M', 41, 'Seeder');
END

-- 2. Локальна таблиця товарів (репліка каталогу для OrdersDB)
IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Id = 1)
BEGIN
    INSERT INTO dbo.Products (Id, Name, Price, Stock, CreatedBy)
    VALUES (1, N'Круасан класичний масляний', 60.00, 50, 'Seeder');
END

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Id = 2)
BEGIN
    INSERT INTO dbo.Products (Id, Name, Price, Stock, CreatedBy)
    VALUES (2, N'Багет традиційний французький', 40.00, 40, 'Seeder');
END

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE Id = 3)
BEGIN
    INSERT INTO dbo.Products (Id, Name, Price, Stock, CreatedBy)
    VALUES (3, N'Чізкейк Сан-Себастьян', 160.00, 20, 'Seeder');
END

-- 3. Замовлення (перевірка за OrderNumber)
DECLARE @Cust1Id INT = (SELECT Id FROM dbo.Customers WHERE Email = 'olena.koval@example.com');
DECLARE @Cust2Id INT = (SELECT Id FROM dbo.Customers WHERE Email = 'taras.shevchuk@example.com');

IF @Cust1Id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderNumber = 'ORD-20260928-001')
BEGIN
    INSERT INTO dbo.Orders (OrderNumber, CustomerId, Status, TotalAmount, OrderDate, Notes, CreatedBy)
    VALUES ('ORD-20260928-001', @Cust1Id, 'Paid', 240.00, '2026-09-28 09:30:00', N'Доставити до 11:00', 'Seeder');
END

IF @Cust2Id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderNumber = 'ORD-20260928-002')
BEGIN
    INSERT INTO dbo.Orders (OrderNumber, CustomerId, Status, TotalAmount, OrderDate, Notes, CreatedBy)
    VALUES ('ORD-20260928-002', @Cust2Id, 'Baking', 160.00, '2026-09-28 10:15:00', N'Без упаковки', 'Seeder');
END

-- 3. Товарні позиції замовлень
DECLARE @Ord1Id INT = (SELECT Id FROM dbo.Orders WHERE OrderNumber = 'ORD-20260928-001');
DECLARE @Ord2Id INT = (SELECT Id FROM dbo.Orders WHERE OrderNumber = 'ORD-20260928-002');

IF @Ord1Id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.OrderItems WHERE OrderId = @Ord1Id AND ProductId = 1)
BEGIN
    INSERT INTO dbo.OrderItems (OrderId, ProductId, ProductName, UnitPrice, Quantity, CreatedBy)
    VALUES (@Ord1Id, 1, N'Круасан класичний масляний', 60.00, 2, 'Seeder');
END

IF @Ord1Id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.OrderItems WHERE OrderId = @Ord1Id AND ProductId = 2)
BEGIN
    INSERT INTO dbo.OrderItems (OrderId, ProductId, ProductName, UnitPrice, Quantity, CreatedBy)
    VALUES (@Ord1Id, 2, N'Багет традиційний французький', 40.00, 3, 'Seeder');
END

IF @Ord2Id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.OrderItems WHERE OrderId = @Ord2Id AND ProductId = 3)
BEGIN
    INSERT INTO dbo.OrderItems (OrderId, ProductId, ProductName, UnitPrice, Quantity, CreatedBy)
    VALUES (@Ord2Id, 3, N'Чізкейк Сан-Себастьян', 160.00, 1, 'Seeder');
END

-- 4. Оплати (1:1 з Orders)
IF @Ord1Id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Payments WHERE OrderId = @Ord1Id)
BEGIN
    INSERT INTO dbo.Payments (OrderId, TransactionNumber, PaymentMethod, Amount, PaymentDate, Status, CreatedBy)
    VALUES (@Ord1Id, 'TXN-98437291-UA', 'ApplePay', 240.00, '2026-09-28 09:32:00', 'Completed', 'Seeder');
END
